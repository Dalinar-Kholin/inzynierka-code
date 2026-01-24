// Browser-compatible (no Node 'crypto', no Buffer). Uses WebCrypto + Uint8Array.

const P_HEX =
    "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" +
    "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B" +
    "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9" +
    "A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE6" +
    "49286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8" +
    "FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D" +
    "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C" +
    "180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF695581718" +
    "3995497CEA956AE515D2261898FA051015728E5A8AACAA68FFFFFFFF" +
    "FFFFFFFF";

const p = BigInt("0x" + P_HEX);
const g = 2n;
const q = (p - 1n) / 2n;

// ---------- bytes utils ----------

const te = new TextEncoder();

function concatBytes(...parts: Uint8Array[]): Uint8Array {
    let total = 0;
    for (const p of parts) total += p.length;
    const out = new Uint8Array(total);
    let off = 0;
    for (const p of parts) {
        out.set(p, off);
        off += p.length;
    }
    return out;
}

function bigintToBytesBE(x: bigint): Uint8Array {
    // matches Go big.Int.Bytes(): big-endian, minimal length, no leading zeros
    if (x === 0n) return new Uint8Array([0]);
    let hex = x.toString(16);
    if (hex.length % 2) hex = "0" + hex;
    const out = new Uint8Array(hex.length / 2);
    for (let i = 0; i < out.length; i++) {
        out[i] = parseInt(hex.slice(i * 2, i * 2 + 2), 16);
    }
    return out;
}

function u64ToBytesBE(x: bigint): Uint8Array {
    // uint64 big-endian
    const out = new Uint8Array(8);
    let v = x;
    for (let i = 7; i >= 0; i--) {
        out[i] = Number(v & 0xffn);
        v >>= 8n;
    }
    return out;
}

function bytesToHex(b: Uint8Array): string {
    let s = "";
    for (const x of b) s += x.toString(16).padStart(2, "0");
    return s;
}

async function sha512Bytes(...parts: (Uint8Array | string)[]): Promise<Uint8Array> {
    const bytesParts = parts.map((p) => (typeof p === "string" ? te.encode(p) : p));
    const msg = concatBytes(...bytesParts);

    // @ts-ignore
    const digest = await crypto.subtle.digest("SHA-512", msg);
    return new Uint8Array(digest);
}

// ---------- bigint math ----------

function modPow(base: bigint, exp: bigint, mod: bigint): bigint {
    if (mod === 1n) return 0n;
    let result = 1n;
    let b = base % mod;
    let e = exp;
    while (e > 0n) {
        if (e & 1n) result = (result * b) % mod;
        e >>= 1n;
        b = (b * b) % mod;
    }
    return result;
}

// ---------- port Go logic (browser async due to WebCrypto) ----------

async function hashToScalar(data: Uint8Array): Promise<bigint> {
    const sum = await sha512Bytes(data);
    let x = BigInt("0x" + bytesToHex(sum)) % q;
    if (x === 0n) x = 1n;
    return x;
}

let cachedH: bigint | null = null;

async function deriveH(): Promise<bigint> {
    if (cachedH !== null) return cachedH; // derive once; same p,g always

    const one = 1n;
    const pMinusOneDivQ = (p - one) / q; // for group14: 2

    let counter = 0n;
    for (;;) {
        const sum = await sha512Bytes(
            "pedersen-h-generator",
            bigintToBytesBE(p),
            bigintToBytesBE(g),
            u64ToBytesBE(counter)
        );

        // t in [1, p-1]
        let t = BigInt("0x" + bytesToHex(sum));
        t = t % (p - one); // [0, p-2]
        t = t + one;       // [1, p-1]

        const h = modPow(t, pMinusOneDivQ, p);
        if (h !== one) {
            cachedH = h;
            return h;
        }
        counter++;
    }
}

/**
 * Browser version is async (WebCrypto).
 */
export async function createCommitment(m: string, r: string): Promise<bigint> {
    const M = await hashToScalar(te.encode("m:" + m));
    const R = await hashToScalar(te.encode("r:" + r));

    const gm = modPow(g, M, p);
    const h = await deriveH();
    const hr = modPow(h, R, p);

    return (gm * hr) % p;
}


export async function verifyCommitment(m: string, r: string, C: bigint): Promise<boolean> {
    return await createCommitment(m, r) === (C % p);
}

export async function verifyCommitmentFromString(
    m: string,
    r: string,
    Cstr: string
): Promise<boolean> {
    const s = Cstr.trim().toLowerCase();
    const C =
        s.startsWith("0x")
            ? BigInt(s)
            : (s.match(/^[0-9]+$/) ? BigInt(s) : BigInt("0x" + s));
    return verifyCommitment(m, r, C);
}

export async function sha256Hex(data: string | Uint8Array): Promise<string> {
    const bytes =
        typeof data === "string"
            ? new TextEncoder().encode(data)
            : data;

    // @ts-ignore
    const digest = await crypto.subtle.digest("SHA-256", bytes);
    const arr = new Uint8Array(digest);

    let hex = "";
    for (const b of arr) {
        hex += b.toString(16).padStart(2, "0");
    }
    return hex;
}