using System.Net.Sockets;
using System.Text;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Same address and port as the server is listening on
            using var client = new TcpClient("127.0.0.1", 5001);
            using var stream = client.GetStream();

            Console.WriteLine("[CLIENT] Connected to the server.");

            // 1) Send a GenerateRequest (type=0)
            Console.WriteLine("[CLIENT] Sending request type = 0 (Generate)...");
            stream.WriteByte(0);

            // Send Count (2 bytes) - generate 5 codes
            byte[] countBytes = BitConverter.GetBytes((ushort)5);
            stream.Write(countBytes, 0, countBytes.Length);
            Console.WriteLine("[CLIENT] Sent Count=5.");

            // Send Length (1 byte) - e.g., 7
            stream.WriteByte(7);
            Console.WriteLine("[CLIENT] Sent Length=7.");

            // 2) Read the server's response

            // Read 1 byte => resultByte (1 = success)
            Console.WriteLine("[CLIENT] Waiting for resultByte...");
            int resultByte = stream.ReadByte();
            Console.WriteLine($"[CLIENT] resultByte={resultByte}");

            if (resultByte == 1)
            {
                // Read the number of codes (2 bytes)
                byte[] codesCountBuf = new byte[2];
                int readCount = stream.Read(codesCountBuf, 0, 2);
                if (readCount < 2)
                {
                    Console.WriteLine("[CLIENT] Not enough data for codesCount!");
                    return;
                }

                ushort nrCodes = BitConverter.ToUInt16(codesCountBuf, 0);
                Console.WriteLine($"[CLIENT] The server says it generated {nrCodes} codes.");

                // Read each code
                for (int i = 0; i < nrCodes; i++)
                {
                    // 1 byte => length of the code
                    int codeLen = stream.ReadByte();
                    if (codeLen == -1)
                    {
                        Console.WriteLine("[CLIENT] No data for code length.");
                        return;
                    }

                    byte[] codeBuf = new byte[codeLen];
                    int codeRead = stream.Read(codeBuf, 0, codeLen);
                    if (codeRead < codeLen)
                    {
                        Console.WriteLine("[CLIENT] Not enough data for the code string.");
                        return;
                    }

                    string code = Encoding.UTF8.GetString(codeBuf);
                    Console.WriteLine($"[CLIENT] Generated code: {code}");
                }
            }
            else
            {
                Console.WriteLine($"[CLIENT] Generation failed. resultByte={resultByte}");
            }

            // 3) (Optional) Send a UseCodeRequest
            //    (type=1)
            Console.WriteLine("[CLIENT] Sending request type = 1 (UseCode)...");
            stream.WriteByte(1);

            // Write 1 byte for the length of the code
            string testCode = "TEST0000"; // One of the generated codes, if known
            byte[] codeBytes = Encoding.UTF8.GetBytes(testCode);
            stream.WriteByte((byte)codeBytes.Length);
            // Now write the actual code
            stream.Write(codeBytes, 0, codeBytes.Length);
            Console.WriteLine($"[CLIENT] Sent code '{testCode}' for usage.");

            // Read the response
            int useCodeResult = stream.ReadByte();
            Console.WriteLine($"[CLIENT] UseCode result: {useCodeResult} (0=Success,1=NotFound,2=Used)");

            Console.WriteLine("[CLIENT] Done. Press ENTER to exit.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENT] Exception: {ex.Message}");
        }
    }
}