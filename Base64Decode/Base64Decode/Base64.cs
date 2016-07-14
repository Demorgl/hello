using System;
using System.Text;

namespace Base64Decode
{
    public class Base64
    {
        public static string Decode(string value)
        {
            var binaryData = Convert.FromBase64String(value);

            return Encoding.UTF8.GetString(binaryData);
        }
    }
}
