using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestEditor_Library
{
    public interface IDrawable
    {
        void Draw(ref float y, Rect inRect,float x);
    }
}
