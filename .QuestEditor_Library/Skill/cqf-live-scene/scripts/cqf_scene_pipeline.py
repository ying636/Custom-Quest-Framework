import argparse
import json
import pathlib
import re
import struct
import time
import uuid
import xml.etree.ElementTree as ET

from cqf_stdio import Client

MAP_TYPE = "QuestEditor_Library.CustomMapDataDef"


def call(client, tool, **arguments):
    request = {"tool": tool, "request_id": uuid.uuid4().hex, "tool_arguments": {key: str(value) for key, value in arguments.items()}}
    result = client.request("tools/call", {"name": "cqf_tool_call", "arguments": request})
    text = "".join(block["text"] for block in result["content"])
    if result.get("isError"):
        raise RuntimeError(text)
    receipt = ET.fromstring(text)
    if receipt.get("success") != "true":
        raise RuntimeError(text)
    return receipt


def request(kind, name, changes=None):
    element = ET.Element("request")
    for key, value in (("kind", kind), ("type", MAP_TYPE), ("name", name), ("changes_xml", changes)):
        if value is not None:
            ET.SubElement(element, key).text = value
    return ET.tostring(element, encoding="unicode")


def validate(client, name):
    targets = call(client, "cqf_list_targets", search=name)
    matches = [target for target in targets.findall(".//target")
               if target.get("name") == name and target.get("type") == MAP_TYPE]
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one editor target for {name}, found {len(matches)}")
    call(client, "cqf_select_target", target_id=matches[0].get("id"))
    receipt = call(client, "cqf_validate_target")
    validation = receipt.find("validation")
    if validation is None or validation.get("passed") != "true":
        raise ValueError(ET.tostring(receipt, encoding="unicode"))
    return receipt


def ready_map(client, scene, prior):
    deadline = time.monotonic() + 80
    while time.monotonic() < deadline:
        status = call(client, "cqf_scene_preview", action="status", name=scene)
        for map_element in status.findall(".//map"):
            if map_element.get("current") == "true" and map_element.get("id") != prior:
                return map_element.get("id")
        time.sleep(0.8)
    raise TimeoutError("CQF did not report a new current preview map")


def capture(client, name, map_id, view, output):
    x, z, zoom = view
    receipt = call(client, "cqf_scene_preview", action="capture", name=name, x=x, z=z, zoom=zoom)
    captured = receipt.find("sceneCapture")
    if captured is None or captured.get("mapId") != map_id or not captured.findtext("path"):
        raise RuntimeError(f"Capture did not match scene preview map {map_id}")
    path = pathlib.Path(captured.findtext("path"))
    deadline = time.monotonic() + 12
    while time.monotonic() < deadline:
        if path.exists():
            data = path.read_bytes()
            if len(data) >= 33 and data[:8] == b"\x89PNG\r\n\x1a\n" and data[-8:-4] == b"IEND":
                width, height = struct.unpack(">II", data[16:24])
                if width > 0 and height > 0:
                    output.write_bytes(data)
                    return
        time.sleep(0.2)
    raise TimeoutError(f"Missing or incomplete PNG: {path}")


def preview(client, name, changes, checks, views, output):
    plan = ET.fromstring(changes)
    if plan.tag != "changes" or plan.attrib or not len(plan):
        raise ValueError("Expected a nonempty <changes> plan")

    operations = list(plan)
    first, rest = operations[:120], operations[120:]
    call(client, "cqf_manage_definition", request_xml=request("create", name, serialize_changes(first)))
    if rest:
        validate(client, name)
        for offset in range(0, len(rest), 120):
            call(client, "cqf_apply_changes", changes_xml=serialize_changes(rest[offset:offset + 120]))
    return inspect(client, name, checks, views, output)


def inspect(client, name, checks, views, output):
    definition_check = validate(client, name)
    ET.ElementTree(definition_check).write(output / "definition-check.xml", encoding="utf-8")

    opened = call(client, "cqf_scene_preview", action="open", name=name).find("scenePreview")
    map_id = ready_map(client, name, opened.get("previousMapId"))
    result = call(client, "cqf_scene_checks", map_id=map_id, checks_xml=checks)
    ET.ElementTree(result).write(output / "scene-checks.xml", encoding="utf-8")
    if result.find("sceneChecks").get("passed") != "true":
        raise RuntimeError(f"Native scene checks failed; see {output / 'scene-checks.xml'}")
    for label, view in views.items():
        capture(client, name, map_id, view, output / f"{label}.png")
    return map_id


def serialize_changes(elements):
    root = ET.Element("changes")
    root.extend(elements)
    return ET.tostring(root, encoding="unicode")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True, type=pathlib.Path)
    parser.add_argument("--pipe", required=True)
    parser.add_argument("--name", required=True)
    subparsers = parser.add_subparsers(dest="action", required=True)
    build = subparsers.add_parser("preview")
    build.add_argument("--changes", required=True, type=pathlib.Path)
    for action in (build, subparsers.add_parser("inspect")):
        for option in ("checks", "views", "output"):
            action.add_argument("--" + option, required=True, type=pathlib.Path)
    export = subparsers.add_parser("export")
    export.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    if not re.fullmatch(r"CQF_[A-Za-z0-9_]{1,100}", args.name):
        parser.error("Use a CQF_ prefixed map defName")
    with Client(args.host.resolve(strict=True), args.pipe) as client:
        if args.action in ("preview", "inspect"):
            views = json.loads(args.views.read_text(encoding="utf-8-sig"))
            if set(views) != {"overview", "detail"} or any(not isinstance(v, list) or len(v) != 3 or any(type(n) is not int for n in v) for v in views.values()):
                parser.error("Views must contain overview and detail as [x, z, zoom]")
            args.output.mkdir(parents=True, exist_ok=True)
            checks = args.checks.read_text(encoding="utf-8-sig")
            if args.action == "preview":
                map_id = preview(client, args.name, args.changes.read_text(encoding="utf-8-sig"), checks, views, args.output)
            else:
                map_id = inspect(client, args.name, checks, views, args.output)
            print(f"Previewed {args.name} on map {map_id}: {args.output}")
        else:
            target = args.output / (args.name + ".xml")
            if target.exists():
                raise FileExistsError(target)
            validate(client, args.name)
            saved = call(client, "cqf_manage_definition", request_xml=request("save", args.name))
            source = pathlib.Path(saved.findtext(".//path"))
            args.output.mkdir(parents=True, exist_ok=True)
            with target.open("xb") as file:
                file.write(source.read_bytes())
            print(target)


if __name__ == "__main__":
    main()
