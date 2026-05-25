using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public interface ISaveable
    {
        XElement SaveToXElement(string nodeName);
    }
}