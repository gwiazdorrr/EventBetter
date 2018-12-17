using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static partial class EventBetter
{
    public static bool Test_IsLeaking
    {
        get
        {
            foreach (var entry in s_entries)
            {
                if (entry.Value.hosts.Any(x => x != null && x.IsAlive))
                    return false;
            }
            return true;
        }
    }
}
