import argparse
import json
import os
import pathlib
import re
import subprocess
import sys
import uuid


class Client:
    def __init__(self, host, pipe):
        self.host = host
        self.pipe = pipe
        self.sequence = 0

    def __enter__(self):
        self.process = subprocess.Popen(
            [self.host, "--pipe", self.pipe],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            encoding="utf-8",
        )
        try:
            info = self.request("initialize", {
                "protocolVersion": "2025-11-25",
                "capabilities": {},
                "clientInfo": {"name": "CQF scene client", "version": "1"},
            })
            if info.get("serverInfo", {}).get("name") != "cqf":
                raise RuntimeError("Expected CQF MCP host")
            self.request("notifications/initialized", notification=True)
        except Exception:
            self.__exit__()
            raise
        return self

    def __exit__(self, *_):
        self.process.stdin.close()
        try:
            self.process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            self.process.terminate()
            self.process.wait(timeout=5)

    def request(self, method, params=None, notification=False):
        message = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            message["params"] = params
        if not notification:
            self.sequence += 1
            message["id"] = self.sequence
        self.process.stdin.write(json.dumps(message, ensure_ascii=False) + "\n")
        self.process.stdin.flush()
        if notification:
            return None
        line = self.process.stdout.readline()
        if not line:
            raise RuntimeError("CQF MCP host closed before returning a result")
        response = json.loads(line)
        if response.get("id") != self.sequence or "error" in response:
            raise RuntimeError(response.get("error", "Unexpected MCP response ID"))
        return response["result"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True, type=pathlib.Path)
    parser.add_argument("--pipe")
    actions = parser.add_subparsers(dest="action", required=True)
    actions.add_parser("status")
    find = actions.add_parser("find")
    find.add_argument("search", nargs="?", default="")
    call = actions.add_parser("call")
    call.add_argument("tool")
    call.add_argument("--arguments-file", type=pathlib.Path)
    call.add_argument("--request-id")
    args = parser.parse_args()
    pipe = args.pipe
    if pipe is None:
        candidates = [entry for entry in os.listdir("\\\\.\\pipe\\")
                      if re.fullmatch(r"CQF-[0-9a-f]{32}", entry, re.I)]
        if len(candidates) != 1:
            raise RuntimeError(f"Expected exactly one CQF bridge, found {len(candidates)}; select --pipe")
        pipe = candidates[0]
    if args.action == "status":
        name, arguments = "cqf_session_get", {}
    elif args.action == "find":
        name, arguments = "cqf_tools_find", {"search": args.search}
    else:
        values = json.loads(args.arguments_file.read_text(encoding="utf-8-sig")) if args.arguments_file else {}
        if not isinstance(values, dict) or any(not isinstance(v, str) for v in values.values()):
            raise ValueError("CQF tool argument values must be strings")
        name = "cqf_tool_call"
        arguments = {"tool": args.tool, "request_id": args.request_id or uuid.uuid4().hex, "tool_arguments": values}
    with Client(args.host.resolve(strict=True), pipe) as client:
        result = client.request("tools/call", {"name": name, "arguments": arguments})
    for block in result["content"]:
        print(block["text"])
    return int(result.get("isError", False))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.exit(main())
    except (OSError, ValueError, RuntimeError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
