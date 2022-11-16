// File : LzssCompressor.cs 
// Creator : Crsky
// Date : 2022/11/16
// Description : A C# port of LZSS.C, implemented by Haruhiko Okumura

namespace AKBTool
{
    public class LzssCompressor
    {
        // size of ring buffer
        private const int N = 4096;

        // upper limit for match_length
        private const int F = 18;

        // encode string into position and length if match_length is greater than this
        private const int THRESHOLD = 2;

        // index for root of binary search trees
        private const int NIL = N;

        // ring buffer of size N, with extra F-1 bytes to facilitate string comparison of longest match.
        private readonly byte[] text_buf;

        // These are set by the InsertNode() procedure.
        private int match_position;
        private int match_length;

        // left & right children & parents -- These constitute binary search trees.
        private readonly int[] lson;
        private readonly int[] rson;
        private readonly int[] dad;

        public LzssCompressor()
        {
            text_buf = new byte[N + F - 1];
            lson = new int[N + 1];
            rson = new int[N + 257];
            dad = new int[N + 1];
        }

        void InitTree()
        {
            // For i = 0 to N - 1, rson[i] and lson[i] will be the right and
            // left children of node i.  These nodes need not be initialized.
            // Also, dad[i] is the parent of node i.  These are initialized to
            // NIL (= N), which stands for 'not used.'
            // For i = 0 to 255, rson[N + i + 1] is the root of the tree
            // for strings that begin with character i.  These are initialized
            // to NIL.  Note there are 256 trees.

            for (int i = N + 1; i <= N + 256; i++)
                rson[i] = NIL;

            for (int i = 0; i < N; i++)
                dad[i] = NIL;
        }

        void InsertNode(int r)
        {
            // Inserts string of length F, text_buf[r..r+F-1], into one of the
            // trees (text_buf[r]'th tree) and returns the longest-match position
            // and length via the global variables match_position and match_length.
            // If match_length = F, then removes the old node in favor of the new
            // one, because the old one will be deleted sooner.
            // Note r plays double role, as tree node and position in buffer

            int cmp = 1;

            int p = N + 1 + text_buf[r];

            rson[r] = NIL;
            lson[r] = NIL;

            match_length = 0;

            while (true)
            {
                if (cmp >= 0)
                {
                    if (rson[p] != NIL)
                        p = rson[p];
                    else
                    {
                        rson[p] = r;
                        dad[r] = p;
                        return;
                    }
                }
                else
                {
                    if (lson[p] != NIL)
                        p = lson[p];
                    else
                    {
                        lson[p] = r;
                        dad[r] = p;
                        return;
                    }
                }

                int i;

                for (i = 1; i < F; i++)
                {
                    cmp = text_buf[r + i] - text_buf[p + i];
                    if (cmp != 0)
                        break;
                }

                if (i > match_length)
                {
                    match_position = p;
                    match_length = i;
                    if (i >= F)
                        break;
                }
            }

            dad[r] = dad[p];
            lson[r] = lson[p];
            rson[r] = rson[p];
            dad[lson[p]] = r;
            dad[rson[p]] = r;

            if (rson[dad[p]] == p)
                rson[dad[p]] = r;
            else
                lson[dad[p]] = r;

            dad[p] = NIL;
        }

        void DeleteNode(int p)
        {
            int q;

            if (dad[p] == NIL)
                return;

            if (rson[p] == NIL)
                q = lson[p];
            else if (lson[p] == NIL)
                q = rson[p];
            else
            {
                q = lson[p];

                if (rson[q] != NIL)
                {
                    do
                    {
                        q = rson[q];
                    } while (rson[q] != NIL);

                    rson[dad[q]] = lson[q];
                    dad[lson[q]] = dad[q];
                    lson[q] = lson[p];
                    dad[lson[p]] = q;
                }

                rson[q] = rson[p];
                dad[rson[p]] = q;
            }

            dad[q] = dad[p];

            if (rson[dad[p]] == p)
                rson[dad[p]] = q;
            else
                lson[dad[p]] = q;

            dad[p] = NIL;
        }

        public byte[] Compress(byte[] input, int capacity)
        {
            var output = new MemoryStream(capacity);

            // initialize trees
            InitTree();

            // code_buf[1..16] saves eight units of code, and
            // code_buf[0] works as eight flags, "1" representing that the unit
            // is an unencoded letter (1 byte), "0" a position-and-length pair
            // (2 bytes).  Thus, eight units require at most 16 bytes of code.
            var codeBuf = new byte[17];
            int codePtr = 1;
            int mask = 1;

            int ptr = 0, i;
            int len;

            // The initial position of buffers.
            int s = 0;
            int r = N - F;

            // Clear the buffer with any character that will appear often.
            for (i = s; i < r; i++)
                text_buf[i] = 0;

            // Read F bytes into the last F bytes of the buffer.
            for (len = 0; len < F && ptr < input.Length; len++, ptr++)
                text_buf[r + len] = input[ptr];

            // Buffer is empty.
            if (len == 0)
                return Array.Empty<byte>();

            // Insert the F strings, each of which begins with one or more 'space' characters.
            // Note the order in which these strings are inserted.
            // This way, degenerate trees will be less likely to occur.
            for (i = 1; i <= F; i++)
                InsertNode(r - i);

            // Finally, insert the whole string just read.
            // The global variables match_length and match_position are set.
            InsertNode(r);

            while (len > 0)
            {
                // match_length may be spuriously long near the end of text.
                if (match_length > len)
                    match_length = len;

                if (match_length <= THRESHOLD)
                {
                    // Not long enough match. Send one byte.
                    match_length = 1;
                    codeBuf[0] |= (byte)mask;
                    codeBuf[codePtr++] = text_buf[r];
                }
                else
                {
                    // Send position and length pair. Note match_length > THRESHOLD.
                    codeBuf[codePtr++] = (byte)match_position;
                    codeBuf[codePtr++] = (byte)(((match_position >> 4) & 0xF0) | (match_length - (THRESHOLD + 1)));
                }

                // Shift mask left one bit.
                mask <<= 1;

                // Send at most 8 units of code together.
                if (mask == 0x100)
                {
                    output.Write(codeBuf, 0, codePtr);

                    // Reset code buffer.
                    codeBuf[0] = 0;
                    codePtr = 1;
                    mask = 1;
                }

                int last_match_length = match_length;

                for (i = 0; i < last_match_length && ptr < input.Length; i++, ptr++)
                {
                    // Read one byte from input.
                    var c = input[ptr];

                    // Delete old strings and read new bytes.
                    DeleteNode(s);
                    text_buf[s] = c;

                    // If the position is near the end of buffer, extend the buffer to make string comparison easier.
                    if (s < F - 1)
                        text_buf[s + N] = c;

                    // Since this is a ring buffer, increment the position modulo N.
                    s = (s + 1) & (N - 1);
                    r = (r + 1) & (N - 1);

                    // Register the string in text_buf[r..r+F-1]
                    InsertNode(r);
                }

                // After the end of text, no need to read, but buffer may not be empty.
                while (i++ < last_match_length)
                {
                    DeleteNode(s);

                    s = (s + 1) & (N - 1);
                    r = (r + 1) & (N - 1);

                    if (--len != 0)
                        InsertNode(r);
                }
            }

            // Send remaining code.
            if (codePtr > 1)
                output.Write(codeBuf, 0, codePtr);

            return output.ToArray();
        }
    }
}