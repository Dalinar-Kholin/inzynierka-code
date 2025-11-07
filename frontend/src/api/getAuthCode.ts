import {consts} from "../const.ts";

interface IGetAuthCode{
    authSerial: string,
    bit: boolean,
}

interface IGetAuthCodeInitResponse{
    C: string,
    g: string,
    n: string,
}

interface IGetAuthCodeResponse{
    c0: string,
    c1: string,
    n0: string,
    n1: string,
    X0: string,
    X1: string,
}
export default async function getAuthCode({ authSerial, bit }: IGetAuthCode) {
    if (authSerial === "") return {};

    const initRes: IGetAuthCodeInitResponse = await fetch(consts.API_URL + "/getAuthCodeInit", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ AuthSerial: authSerial }),
    })
        .then(r => { if (!r.ok) throw new Error("bad response"); return r.json(); });

    const pHex = initRes.n;
    const gHex = initRes.g;
    const CHex = initRes.C;

    const n = hexToBigInt(pHex);
    const g = hexToBigInt(gHex);
    const C = hexToBigInt(CHex);

    const r = randScalar(n);
    let A;
    let AHex;
    let inv;
    let B;
    let BHex

    if (bit){
        A = modPow(g, r, n);
        AHex = bigIntToHex(A);
        inv = modInv(A, n);
        B = (C * inv) % n;
        BHex = bigIntToHex(B);
    }else{
        B = modPow(g, r, n);
        BHex = bigIntToHex(B);
        inv = modInv(B, n);
        A =(C * inv) % n
        AHex = bigIntToHex(A);
    }

    const encRes: IGetAuthCodeResponse = await fetch(consts.API_URL + "/getAuthCode", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ A: AHex, B: BHex, AuthSerial: authSerial }),
    }).then(r => r.json());

    const X0Hex = encRes.X0;
    const X1Hex = encRes.X1;
    const X0 = BigInt('0x' + X0Hex);
    const X1 = BigInt('0x' + X1Hex);

    const Z0 = modPow(X0, r, n);
    const Z1 = modPow(X1, r, n);

    const infoStr =
        `p:${pHex}|g:${gHex}|A:${AHex}|B:${BHex}|X0:${X0Hex}|X1:${X1Hex}`;
    const infoBytes = new TextEncoder().encode(infoStr);

    const Z0b = hexToBytes(Z0.toString(16));
    const Z1b = hexToBytes(Z1.toString(16));

    const salt = new Uint8Array();

    const k0 = await hkdfSha256(Z0b, salt, infoBytes, 32);
    const k1 = await hkdfSha256(Z1b, salt, infoBytes, 32);

    const n0 = hexToBytes(encRes.n0);
    const n1 = hexToBytes(encRes.n1);
    const c0 = hexToBytes(encRes.c0);
    const c1 = hexToBytes(encRes.c1);

    const m0 = await decryptGCM(k0, n0, infoBytes, c0).catch(() => null);
    const m1 = await decryptGCM(k1, n1, infoBytes, c1).catch(() => null);

    const text0 = m0 ? bytesToUtf8(m0) : null;
    const text1 = m1 ? bytesToUtf8(m1) : null;

    console.log(text0)
    console.log(text1)

    return { result: (bit ? text0 : text1) as string };
}

async function hkdfSha256(ikm: Uint8Array, salt: Uint8Array, info: Uint8Array, length = 32): Promise<Uint8Array> {
    const key = await crypto.subtle.importKey('raw', toArrayBufferStrict(ikm), 'HKDF', false, ['deriveBits']);
    const bits = await crypto.subtle.deriveBits(
        { name: 'HKDF', hash: 'SHA-256', salt, info },
        key,
        length * 8
    );
    return new Uint8Array(bits);
}

async function importAesGcmKey(keyBytes: Uint8Array) {
    return crypto.subtle.importKey('raw', toArrayBufferStrict(keyBytes), { name: 'AES-GCM' }, false, ['decrypt']);
}

async function decryptGCM(keyBytes: Uint8Array, nonce: Uint8Array, aad: Uint8Array, ct: Uint8Array): Promise<Uint8Array> {
    const key = await importAesGcmKey(keyBytes);
    const pt = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: nonce, additionalData: aad }, key, ct);
    return new Uint8Array(pt);
}


const hexToBytes = (h: string) => new Uint8Array(h.match(/.{1,2}/g)!.map(b => parseInt(b, 16)));
const bytesToUtf8 = (b: Uint8Array) => new TextDecoder().decode(b);


function toArrayBufferStrict(bs: Uint8Array): ArrayBuffer {
    if (bs instanceof ArrayBuffer) return bs;                // OK
    // bs jest ArrayBufferView → kopiujemy okno do nowego ArrayBuffer
    const view = bs as ArrayBufferView;
    const u8 = new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
    return u8.slice().buffer;                                // nowy ArrayBuffer
}

function hexToBigInt(h: string): bigint { return BigInt('0x' + h.replace(/^0x/, '')); }
function bigIntToHex(x: bigint): string { return x.toString(16); }
const bytesToHex = (b: Uint8Array) => Array.from(b).map(x => x.toString(16).padStart(2,"0")).join("");

function randScalar(p: bigint): bigint {
    const bytes = new Uint8Array(64);
    crypto.getRandomValues(bytes);
    const x = hexToBigInt(bytesToHex(bytes));
    return (x % (p - 2n)) + 1n;
}

function modPow(base: bigint, exp: bigint, mod: bigint): bigint {
    let b = base % mod, e = exp, r = 1n;
    while (e > 0n) { if (e & 1n) r = (r * b) % mod; b = (b * b) % mod; e >>= 1n; }
    return r;
}

function modInv(a: bigint, mod: bigint): bigint {
    // Extended Euclid for a^{-1} mod m (assumes gcd(a,m)=1)
    let t = 0n, nt = 1n, r = mod, nr = a % mod;
    while (nr !== 0n) { const q = r / nr; [t, nt] = [nt, t - q*nt]; [r, nr] = [nr, r - q*nr]; }
    if (r !== 1n) throw new Error("not invertible");
    return t < 0n ? t + mod : t;
}
