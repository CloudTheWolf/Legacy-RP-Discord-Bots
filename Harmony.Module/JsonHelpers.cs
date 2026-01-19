using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public static class JsonHelpers
{
    public static JArray ToJArray(IEnumerable<dynamic> items)
    {
        var arr = new JArray();

        if (items == null)
            return arr;

        foreach (var item in items)
        {
            if (item is JToken jt)
                arr.Add(jt);
            else
                arr.Add(JToken.FromObject(item));
        }

        return arr;
    }
}
