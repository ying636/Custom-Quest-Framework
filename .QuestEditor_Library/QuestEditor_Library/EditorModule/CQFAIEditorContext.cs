using System;
namespace QuestEditor_Library
{
    public sealed class CQFAIEditorContext
    {
        public CQFAIEditorContext(string name, Func<object> read, Action<object> apply, Action<object>? validate = null, Func<bool>? isValid = null, object? owner = null, object? identity = null)
        {
            Name = name;
            Read = read;
            Apply = apply;
            Validate = validate;
            IsValid = isValid;
            Owner = owner;
            this.identity = identity;
        }
        public string Name { get; }
        public Func<object> Read { get; }
        public Action<object> Apply { get; }
        public Action<object>? Validate { get; }
        public Func<bool>? IsValid { get; }
        public object? Owner { get; }
        public object Identity => identity ?? Read();
        private readonly object? identity;
    }
}
