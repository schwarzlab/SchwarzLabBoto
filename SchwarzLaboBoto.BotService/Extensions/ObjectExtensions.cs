using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SchwarzLaboBoto.BotService.Extensions
{
    internal static class ObjectExtensions
    {
        internal static T ToObject<T>(this IDictionary<string, string> source)
            where T : class, new()
        {
            var someObj = new T();
            var someObjType = someObj.GetType();

            foreach (var item in source)
            {
                someObjType.GetProperty(item.Key)
                    .SetValue(someObj, item.Value, null);
            }
            return someObj;
        }

    }
}
