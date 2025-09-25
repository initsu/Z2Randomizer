// See https://aka.ms/new-console-template for more information

using Z2Randomizer.RandomizerCore;

string VANILLA_ROM_PATH = @"C:\emu\NES\roms\Zelda2.nes";
string spritesFolder = @"O:\source\Z2Randomizer\RandomizerCore\Sprites";

byte[] vanillaRomData = File.ReadAllBytes(VANILLA_ROM_PATH);


var files = Directory.GetFiles(spritesFolder, "*.ips");
foreach (var f in files)
{
    RecreateSanitizedPatch(vanillaRomData, f, f);
}



#pragma warning disable CS8321 // Local function is declared but never used
static void SetCredit(ROM rom, string credit, string credit2="")
{
    if (credit.Length > 0x1c) { throw new ArgumentException($"credit string too long, {credit.Length}"); }
    rom.Put(0x16abb, ROM.StringToZ2Bytes(credit.ToUpper().PadCenter(0x1c)));
    if (credit2.Length > 0x1c) { throw new ArgumentException($"credit2 string too long, {credit2.Length}"); }
    rom.Put(0x16ad8, ROM.StringToZ2Bytes(credit2.ToUpper().PadCenter(0x1c)));
}

static void RecreateSanitizedPatch(byte[] vanillaRomData, string filename, string outputFilename)
{
    byte[] ipsData = File.ReadAllBytes(filename);
    byte[] romData = [.. vanillaRomData];

    SpritePatcher.PatchSpriteSanitized(filename, romData, ipsData, false, true, true, false, false);

    var rom = new ROM(romData);
    //SetCredit(rom, "Sprite by chibiplan");
    //SetCredit(rom, "Sprite by Jackimus");
    //SetCredit(rom, "Sprite by Irenepunmaster");
    //SetCredit(rom, "Sprite by Schmiddty,", "Irenepunmaster and VTSlacker");
    //SetCredit(rom, "Sprite by Irenepunmaster", "and VTSlacker");
    //SetCredit(rom, "Sprite by VTSlacker");
    //SetCredit(rom, "Sprite by Mister Mike");
    //SetCredit(rom, "Sprite by Knightcrawler");
    //SetCredit(rom, "Sprite by Mister Mike", "and VTSlacker");
    //SetCredit(rom, "Sprite by Lord Louie,", "Z-9 Lurker and Mister Mike");
    SetCredit(rom, "Sprite by valence", "From Street Cleaner 3");

    byte[] finalPatch = IpsPatcher.CreateIpsPatch(vanillaRomData, romData, false);

    File.WriteAllBytes(outputFilename, finalPatch);
}
#pragma warning restore CS8321 // Local function is declared but never used



public static class IpsPatcher
{
    // Create an IPS patch byte array from original and modified byte arrays.
    // If allowTruncate is true and modified is shorter than original, a 3-byte
    // truncate value will be appended after EOF (extension supported by many patchers).
    public static byte[] CreateIpsPatch(byte[] original, byte[] modified, bool allowTruncate = true)
    {
        if (original == null) original = Array.Empty<byte>();
        if (modified == null) modified = Array.Empty<byte>();

        var outBuf = new MemoryStream();
        // Write header "PATCH"
        outBuf.Write(new byte[] { (byte)'P', (byte)'A', (byte)'T', (byte)'C', (byte)'H' }, 0, 5);

        int maxIndex = Math.Max(original.Length, modified.Length);

        int i = 0;
        while (i < maxIndex)
        {
            bool differs;
            if (i >= modified.Length)
            {
                // modified is shorter -> bytes beyond modified are implicitly different
                differs = true;
            }
            else if (i >= original.Length)
            {
                // original doesn't have this byte -> it's different
                differs = true;
            }
            else
            {
                differs = original[i] != modified[i];
            }

            if (!differs)
            {
                i++;
                continue;
            }

            // Find the end of this differing region in 'modified' terms (j is exclusive)
            int j = i;
            while (j < modified.Length)
            {
                bool diffAtJ = (j >= original.Length) || (original[j] != modified[j]);
                if (!diffAtJ) break;
                j++;
            }

            // If modified ended (j == modified.Length) but original has extra bytes beyond
            // modified, we still process the differing region [i, j) ; truncation is handled separately.
            // Now encode the differing region [i, j) as one or more IPS records.
            // We'll walk k through [i,j) and for repeated-byte runs use RLE where beneficial.
            int k = i;
            while (k < j)
            {
                // Determine run length of same byte in modified starting at k
                byte runByte = modified[k];
                int runLen = 1;
                while (k + runLen < j && modified[k + runLen] == runByte && runLen < 0xFFFF) runLen++;

                // Heuristic: use RLE if the run is at least 3 bytes long (saves space often).
                // You can adjust threshold as desired.
                const int RLE_THRESHOLD = 3;
                if (runLen >= RLE_THRESHOLD)
                {
                    // If runLen may be larger than 0xFFFF, we already limited it above; if remaining
                    // run > 0xFFFF we'll loop and emit additional RLE records.
                    WriteRleRecord(outBuf, k, runLen, runByte);
                    k += runLen;
                }
                else
                {
                    // Emit a raw block. We should accumulate as large a contiguous non-RLE block
                    // as possible up to 0xFFFF.
                    int rawStart = k;
                    int rawLen = 0;
                    while (k < j && rawLen < 0xFFFF)
                    {
                        // if this next byte would start a sufficiently long RLE, break to emit RLE next
                        if (k + RLE_THRESHOLD <= j)
                        {
                            byte candidate = modified[k];
                            int candidateRun = 1;
                            while (k + candidateRun < j && modified[k + candidateRun] == candidate && candidateRun < 0xFFFF)
                                candidateRun++;
                            if (candidateRun >= RLE_THRESHOLD && rawLen > 0)
                                break; // let the next loop iteration handle the RLE
                        }
                        // consume one byte into the raw block
                        k++;
                        rawLen++;
                    }

                    // Now write the raw record for [rawStart, rawStart+rawLen)
                    WriteRawRecord(outBuf, rawStart, modified, rawStart, rawLen);
                }
            }

            // move i past this differing region
            i = j;
        }

        // Write EOF
        outBuf.Write(new byte[] { (byte)'E', (byte)'O', (byte)'F' }, 0, 3);

        // Optional truncate extension: many IPS patchers support a 3-byte size after EOF
        if (allowTruncate && modified.Length < original.Length)
        {
            Write3ByteBigEndian(outBuf, modified.Length);
        }

        return outBuf.ToArray();
    }

    // Write a raw (non-RLE) IPS record:
    // [3-byte offset][2-byte size][data...]
    private static void WriteRawRecord(Stream s, int offset, byte[] src, int srcIndex, int length)
    {
        // Split records larger than 0xFFFF
        int remaining = length;
        int curSrcIdx = srcIndex;
        int curOffset = offset;
        while (remaining > 0)
        {
            int chunk = Math.Min(remaining, 0xFFFF);
            Write3ByteBigEndian(s, curOffset);
            Write2ByteBigEndian(s, chunk);
            s.Write(src, curSrcIdx, chunk);

            remaining -= chunk;
            curSrcIdx += chunk;
            curOffset += chunk;
        }
    }

    // Write an RLE record:
    // [3-byte offset][2-byte size==0][2-byte rleSize][1-byte value]
    private static void WriteRleRecord(Stream s, int offset, int rleLength, byte value)
    {
        int remaining = rleLength;
        int curOffset = offset;
        while (remaining > 0)
        {
            int chunk = Math.Min(remaining, 0xFFFF);
            Write3ByteBigEndian(s, curOffset);
            // size==0 indicates RLE record
            Write2ByteBigEndian(s, 0);
            Write2ByteBigEndian(s, chunk);
            s.WriteByte(value);

            remaining -= chunk;
            curOffset += chunk;
        }
    }

    private static void Write3ByteBigEndian(Stream s, int value)
    {
        if (value < 0 || value > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(value));
        s.WriteByte((byte)((value >> 16) & 0xFF));
        s.WriteByte((byte)((value >> 8) & 0xFF));
        s.WriteByte((byte)(value & 0xFF));
    }

    private static void Write2ByteBigEndian(Stream s, int value)
    {
        if (value < 0 || value > 0xFFFF) throw new ArgumentOutOfRangeException(nameof(value));
        s.WriteByte((byte)((value >> 8) & 0xFF));
        s.WriteByte((byte)(value & 0xFF));
    }
}

public static class StringExtensions
{
    public static string PadCenter(this string text, int width)
    {
        if (string.IsNullOrEmpty(text) || text.Length >= width)
            return text;

        int spaces = width - text.Length;
        int padLeft = spaces / 2;

        return $"{new string(' ', padLeft)}{text}".PadRight(width);
    }
}
