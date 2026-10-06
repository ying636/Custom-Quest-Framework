using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogEditSession
    {
        public bool CanUndo => this.undo.Count > 0;
        public bool CanRedo => this.redo.Count > 0;

        public void Reset(DialogTreeDef tree)
        {
            this.undo.Clear();
            this.redo.Clear();
            this.Remember(tree);
        }

        public void Observe(DialogTreeDef tree)
        {
            string xml = tree.SaveToXElement("tree").ToString(SaveOptions.DisableFormatting);
            if (xml == this.lastXml)
            {
                return;
            }
            if (this.lastTree != null)
            {
                this.undo.Add(this.lastTree);
                if (this.undo.Count > 40)
                {
                    this.undo.RemoveAt(0);
                }
            }
            this.redo.Clear();
            this.Remember(tree);
        }

        public DialogTreeDef Undo(DialogTreeDef current)
        {
            this.Observe(current);
            this.redo.Add(this.Copy(current));
            DialogTreeDef result = this.undo[this.undo.Count - 1];
            this.undo.RemoveAt(this.undo.Count - 1);
            this.Remember(result);
            return result;
        }

        public DialogTreeDef Redo(DialogTreeDef current)
        {
            this.undo.Add(this.Copy(current));
            DialogTreeDef result = this.redo[this.redo.Count - 1];
            this.redo.RemoveAt(this.redo.Count - 1);
            this.Remember(result);
            return result;
        }

        public DialogTreeDef Copy(DialogTreeDef tree)
        {
            DialogTreeDef result = (DialogTreeDef)this.Clone(tree, new Dictionary<object, object>(), true)!;
            result.Update();
            return result;
        }

        private void Remember(DialogTreeDef tree)
        {
            this.lastXml = tree.SaveToXElement("tree").ToString(SaveOptions.DisableFormatting);
            this.lastTree = this.Copy(tree);
        }

        private object? Clone(object? value, Dictionary<object, object> copies, bool root = false)
        {
            if (value == null)
            {
                return null;
            }
            Type type = value.GetType();
            if (type.IsValueType || value is string || value is Type || value is Delegate
                || value is UnityEngine.Object || (!root && value is Def))
            {
                return value;
            }
            if (copies.TryGetValue(value, out object existing))
            {
                return existing;
            }
            if (value is Array array)
            {
                Array copy = (Array)array.Clone();
                copies.Add(value, copy);
                for (int i = 0; i < array.Length; i++)
                {
                    copy.SetValue(this.Clone(array.GetValue(i), copies), i);
                }
                return copy;
            }
            if (value is IDictionary dictionary)
            {
                IDictionary copy = (IDictionary)Activator.CreateInstance(type);
                copies.Add(value, copy);
                foreach (DictionaryEntry pair in dictionary)
                {
                    copy.Add(this.Clone(pair.Key, copies), this.Clone(pair.Value, copies));
                }
                return copy;
            }
            if (value is IList list)
            {
                IList copy = (IList)Activator.CreateInstance(type);
                copies.Add(value, copy);
                foreach (object item in list)
                {
                    copy.Add(this.Clone(item, copies));
                }
                return copy;
            }
            object result = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null);
            copies.Add(value, result);
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!field.IsInitOnly)
                    {
                        field.SetValue(result, this.Clone(field.GetValue(value), copies));
                    }
                }
            }
            return result;
        }

        private readonly List<DialogTreeDef> undo = new List<DialogTreeDef>();
        private readonly List<DialogTreeDef> redo = new List<DialogTreeDef>();
        private DialogTreeDef? lastTree;
        private string lastXml = string.Empty;
    }
}
