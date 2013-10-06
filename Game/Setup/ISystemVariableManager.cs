﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Setup
{
    public interface ISystemVariableManager
    {
        SystemVariable this[string index] { get; set; }
        bool TryGetValue(string key, out SystemVariable systemVariable);
        bool ContainsKey(string key);
        void Add(string key, SystemVariable systemVariable);
        void Clear();
    }
}
