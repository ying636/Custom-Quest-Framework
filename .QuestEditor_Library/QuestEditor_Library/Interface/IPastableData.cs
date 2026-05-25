using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestEditor_Library
{
    public interface IPastableData
    {
        void PasteData();
    }

    public interface ICopiableData
    {
        void CopyData();
    }
}
