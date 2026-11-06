using System;

namespace QuestEditor_Library
{
    public readonly struct CQFSymmetryScope : IDisposable
    {
        public CQFSymmetryScope(CQFEditorSymmetry component)
        {
            this.component = component;
            component.BeginOperation();
        }

        public void Dispose() => this.component.EndOperation();

        private readonly CQFEditorSymmetry component;
    }
}
