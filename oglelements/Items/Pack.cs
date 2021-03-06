﻿using OGL.Base;
using OGL.Common;
using OGL.Keywords;
using System;
using System.Collections.Generic;

namespace OGL.Items
{
    public class Pack: Item, IOGLElement<Pack>
    {
        public List<string> Contents;
        public Pack(): base()
        {
            Contents = new List<string>();
        }
        public Pack(OGLContext context, String name, String description, Price price, List<Item> contents, Keyword kw1 = null, Keyword kw2 = null, Keyword kw3 = null, Keyword kw4 = null, Keyword kw5 = null, Keyword kw6 = null, Keyword kw7 = null)
            : base(context, name, description, price, 0, 1, kw1, kw2, kw3, kw4, kw5, kw6, kw7)
        {
            Contents = new List<string>();
            foreach (Item i in contents)
            {
                Contents.Add(i.Name);
                Weight += i.Weight;
            }
        }

        Pack IOGLElement<Pack>.Clone()
        {
            return base.Clone() as Pack;
        }
    }
}
