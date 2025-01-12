using System.Net;
using System.Net.Sockets;
using System.Text;
using DiscountCodeServer;

internal class Program
{
    private static DiscountManager _discountManager;

    static void Main(string[] args)
    {
        _discountManager = new DiscountManager();

        // Start a TcpListener on port 5001 (you can choose a different port if you like)
        TcpListener listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        Console.WriteLine("Server started on port 5001.");

        // Accept incoming connections in a loop
        while (true)
        {
            var client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            // Run each connection in a separate task (to handle multiple requests in parallel)
            Task.Run(() => HandleClient(client));
        }
    }

    private static void HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        try
        {
            // Read requests in a loop until the client disconnects (requestType == -1)
            while (true)
            {
                int requestType = stream.ReadByte();
                if (requestType == -1) return; // No more data

                switch (requestType)
                {
                    case 0:
                        HandleGenerateRequest(stream);
                        break;
                    case 1:
                        HandleUseCodeRequest(stream);
                        break;
                    default:
                        Console.WriteLine("Unknown request type.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex}");
        }
        finally
        {
            client.Close();
        }
    }

    private static void HandleGenerateRequest(NetworkStream stream)
    {
        // 2 bytes = Count (ushort)
        byte[] countBuffer = new byte[2];
        stream.Read(countBuffer, 0, 2);
        ushort count = BitConverter.ToUInt16(countBuffer, 0);

        // Check if count > 2000
        if (count > 2000)
        {
            // For example, we can send a "fail" byte (0) and return
            stream.WriteByte(0);
            Console.WriteLine("GenerateRequest failed: too many codes requested (max 2000).");
            return;
        }

        // 1 byte = Length (byte)
        int length = stream.ReadByte();

        // Check if length is 7 or 8
        if (length < 7 || length > 8)
        {
            // Send a fail byte (0) and return
            stream.WriteByte(0);
            Console.WriteLine("GenerateRequest failed: length must be 7 or 8.");
            return;
        }

        Console.WriteLine($"GenerateRequest: Count={count}, Length={length}");

        // Call discountManager to generate codes
        var codes = _discountManager.GenerateCodes(count, length);

        // Send response: 1 byte for "success" (e.g. 1)
        stream.WriteByte(1);

        // Then send the number of generated codes (2 bytes)
        byte[] codesCountBuf = BitConverter.GetBytes((ushort)codes.Count);
        stream.Write(codesCountBuf, 0, 2);

        // Send each code: first one byte for its length, then the code itself
        foreach (var code in codes)
        {
            byte[] codeBytes = Encoding.UTF8.GetBytes(code);
            stream.WriteByte((byte)codeBytes.Length);
            stream.Write(codeBytes, 0, codeBytes.Length);
        }
    }

    private static void HandleUseCodeRequest(NetworkStream stream)
    {
        // 1 byte = code length
        int codeLength = stream.ReadByte();
        if (codeLength == -1)
        {
            Console.WriteLine("UseCodeRequest failed: no code length received.");
            return;
        }

        // Read 'codeLength' bytes for the code
        byte[] codeBuf = new byte[codeLength];
        stream.Read(codeBuf, 0, codeLength);

        string code = Encoding.UTF8.GetString(codeBuf);
        Console.WriteLine($"UseCodeRequest: Code={code}");

        // Call discountManager to use the code
        var result = _discountManager.UseCode(code);

        // Response: 1 byte = (0=Success, 1=NotFound, 2=AlreadyUsed)
        stream.WriteByte((byte)result);
    }
}