using System;
using System.Security.Cryptography;

namespace DiscountCodeServer
{
    public class DiscountManager
    {
        private static readonly object _lockObj = new object();
        protected string _filePath = "discountCodes.json";

        protected List<DiscountCode> _codes;

        public DiscountManager()
        {
            // Load existing codes from file (or DB) at startup
            _codes = LoadCodesFromFile();
        }

        // Method that generates discount codes
        public List<string> GenerateCodes(int count, int length)
        {
            lock (_lockObj)
            {
                var generated = new List<string>();

                // Generate codes up to 'count' times
                for (int i = 0; i < count; i++)
                {
                    string code;
                    do
                    {
                        code = GenerateDiscountCode(length);
                    } while (_codes.Any(c => c.Code == code));

                    _codes.Add(new DiscountCode { Code = code, IsUsed = false });
                    generated.Add(code);
                }

                // Save after generating new codes
                SaveCodesToFile();
                return generated;
            }
        }

        // Method that marks a code as used
        public UseCodeResult UseCode(string code)
        {
            lock (_lockObj)
            {
                var discount = _codes.FirstOrDefault(c => c.Code == code);
                if (discount == null)
                {
                    return UseCodeResult.CodeNotFound;
                }
                if (discount.IsUsed)
                {
                    return UseCodeResult.AlreadyUsed;
                }
                discount.IsUsed = true;
                SaveCodesToFile();
                return UseCodeResult.Success;
            }
        }

        // Generates a random discount code of the specified length
        private string GenerateDiscountCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            byte[] data = new byte[length];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }
            return new string(result);
        }

        // Loads codes from a local JSON file
        protected List<DiscountCode> LoadCodesFromFile()
        {
            if (!File.Exists(_filePath))
                return new List<DiscountCode>();

            var json = File.ReadAllText(_filePath);

            // Handle empty or invalid JSON gracefully
            if (string.IsNullOrWhiteSpace(json))
                return new List<DiscountCode>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<DiscountCode>>(json)
                       ?? new List<DiscountCode>();
            }
            catch (System.Text.Json.JsonException)
            {
                // If the file contains invalid JSON, treat it as empty
                return new List<DiscountCode>();
            }
        }

        // Saves codes to the local JSON file
        private void SaveCodesToFile()
        {
            lock (_lockObj)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_codes);
                File.WriteAllText(_filePath, json);
            }
        }
    }

    // Model class for a discount code
    public class DiscountCode
    {
        public string Code { get; set; }
        public bool IsUsed { get; set; }
    }

    // Enum for "UseCode" results
    public enum UseCodeResult : byte
    {
        Success = 0,
        CodeNotFound = 1,
        AlreadyUsed = 2
    }
}

