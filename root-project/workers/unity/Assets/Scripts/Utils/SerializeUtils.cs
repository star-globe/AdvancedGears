using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace AdvancedGears
{
    static class SerializeUtils
    {
        public static byte[] SerializeArguments(object playerCreationArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, playerCreationArguments);
                return memoryStream.ToArray();
            }
        }

        public static T DeserializeArguments<T>(byte[] serializedArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                memoryStream.Write(serializedArguments, 0, serializedArguments.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T) binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}