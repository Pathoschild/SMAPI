using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace StardewModdingAPI.Web.Framework.Compression;

/// <summary>Handles GZip compression logic.</summary>
internal class GzipHelper : IGzipHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>The first bytes in a valid zip file.</summary>
    /// <remarks>See <a href="https://en.wikipedia.org/wiki/Zip_(file_format)#File_headers"/>.</remarks>
    private const uint GzipLeadBytes = 0x8b1f;


    /*********
    ** Public methods
    *********/
    /// <summary>Compress a string.</summary>
    /// <param name="text">The text to compress.</param>
    /// <remarks>Derived from <a href="https://stackoverflow.com/a/17993002/262123"/>.</remarks>
    public string CompressString(string text)
    {
        // get raw bytes
        byte[] buffer = Encoding.UTF8.GetBytes(text);

        // compressed
        byte[] compressedData;
        using (MemoryStream stream = new())
        {
            using (GZipStream zipStream = new(stream, CompressionLevel.Optimal, leaveOpen: true))
                zipStream.Write(buffer, 0, buffer.Length);

            stream.Position = 0;
            compressedData = new byte[stream.Length];
            stream.Read(compressedData, 0, compressedData.Length);
        }

        // prefix length
        byte[] zipBuffer = new byte[compressedData.Length + 4];
        Buffer.BlockCopy(compressedData, 0, zipBuffer, 4, compressedData.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, zipBuffer, 0, 4);

        // return string representation
        return Convert.ToBase64String(zipBuffer);
    }

    /// <summary>Decompress a string.</summary>
    /// <param name="rawText">The compressed text.</param>
    /// <remarks>Derived from <a href="https://stackoverflow.com/a/17993002/262123"/>.</remarks>
    [return: NotNullIfNotNull("rawText")]
    public string? DecompressString(string? rawText)
    {
        if (rawText is null)
            return rawText;

        // get raw bytes
        byte[] zipBuffer;
        try
        {
            zipBuffer = Convert.FromBase64String(rawText);
        }
        catch
        {
            return rawText; // not valid base64, wasn't compressed by the log parser
        }

        // skip if not gzip
        if (BitConverter.ToUInt16(zipBuffer, 4) != GzipHelper.GzipLeadBytes)
            return rawText;

        // decompress
        using MemoryStream memoryStream = new();
        {
            // read length prefix
            int dataLength = BitConverter.ToInt32(zipBuffer, 0);
            memoryStream.Write(zipBuffer, 4, zipBuffer.Length - 4);

            // read data
            byte[] buffer = new byte[dataLength];
            memoryStream.Position = 0;
            using (GZipStream gZipStream = new(memoryStream, CompressionMode.Decompress))
                gZipStream.Read(buffer, 0, buffer.Length);

            // return original string
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
