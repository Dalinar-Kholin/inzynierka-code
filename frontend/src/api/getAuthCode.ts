import {consts} from "../helpers/const.ts";
import {
    type FetchWithAuthFnType,
    IsBadSignError,
    IsServerError,
} from "../hooks/useFetchWithVerify.ts";
import useFetchWithVerify from "../hooks/useFetchWithVerify.ts";

interface IGetAuthCode{
    authSerial: string,
    bit: boolean,
}

interface IGetAuthCodeInitResponse{
    c: string,
    g: string,
    n: string,
}


interface otData{
    c0: string,
    c1: string,
    n0: string,
    n1: string,
    x0: string,
    x1: string,
}

interface IGetAuthCodeResponse{
    otData: otData;
    permCode: string;
    r: string;
}

export interface IOtResponse {
    authCode : string
    permCode? : string
    r?: string,
}

interface IGetAuthCodeInitRequest{
    authSerial: string,
}

interface IGetAuthCodeRequest{
    a: string,
    b: string,
    authSerial: string
}

export default function useGetAuthCode(){
    const fetchWithAuth = useFetchWithVerify()

    async function getCode({ authSerial, bit }: IGetAuthCode): Promise<IOtResponse>{
        return await getAuthCode({authSerial, bit, fetchWithAuth})
    }

    return {
        getCode,
    }
}

interface IGetAuthCodeHelper{
    authSerial: string,
    bit: boolean,
    fetchWithAuth: FetchWithAuthFnType
}

async function getAuthCode({ authSerial, bit, fetchWithAuth } : IGetAuthCodeHelper) : Promise<IOtResponse> {
    if (authSerial === "") return {authCode: ""};


    let response = await fetchWithAuth<IGetAuthCodeInitResponse, IGetAuthCodeInitRequest>(consts.API_URL + "/getAuthCodeInit", {
        method: "POST",
        headers: { "content-type": "application/json" },
    }, { authSerial: authSerial })

    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error(response.error);
    }

    const pHex = response.n;
    const gHex = response.g;
    const CHex = response.c;

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
    const secResponse = await fetchWithAuth<IGetAuthCodeResponse, IGetAuthCodeRequest>(consts.API_URL + "/getAuthCode", {
        method: "POST",
        headers: { "content-type": "application/json" },
    }, { a: AHex, b: BHex, authSerial: authSerial })

    if (IsBadSignError(secResponse)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(secResponse)) {
        throw new Error(secResponse.error);
    }

    const X0Hex = secResponse.otData.x0;
    const X1Hex = secResponse.otData.x1;
    const X0 = BigInt('0x' + X0Hex);
    const X1 = BigInt('0x' + X1Hex);

    const Z0 = modPow(X0, r, n);
    const Z1 = modPow(X1, r, n);

    const infoStr =
        `p:${pHex}|g:${gHex}|A:${AHex}|B:${BHex}|X0:${X0Hex}|X1:${X1Hex}`;
    const infoBytes = new TextEncoder().encode(infoStr);

    const pByteLen = Math.ceil(pHex.length / 2);

    const Z0b = bigintToFixedBytes(Z0, pByteLen);
    const Z1b = bigintToFixedBytes(Z1, pByteLen);

    const salt = new Uint8Array();

    const k0 = await hkdfSha256(Z0b, salt, infoBytes, 32);
    const k1 = await hkdfSha256(Z1b, salt, infoBytes, 32);

    const n0 = hexToBytes(secResponse.otData.n0);
    const n1 = hexToBytes(secResponse.otData.n1);
    const c0 = hexToBytes(secResponse.otData.c0);
    const c1 = hexToBytes(secResponse.otData.c1);

    const m0 = await decryptGCM(k0, n0, infoBytes, c0).catch(() => null);
    const m1 = await decryptGCM(k1, n1, infoBytes, c1).catch(() => null);

    const text0 = m0 ? bytesToUtf8(m0) : null;
    const text1 = m1 ? bytesToUtf8(m1) : null;

    return { authCode: (bit ? text0 : text1) as string, permCode:secResponse.permCode, r: secResponse.r };
}

function hexToBytesEven(h: string): Uint8Array {
    const s = h.length % 2 ? "0" + h : h;
    const out = new Uint8Array(s.length / 2);
    for (let i = 0; i < out.length; i++) out[i] = parseInt(s.slice(2*i, 2*i+2), 16);
    return out;
}

function bigintToFixedBytes(x: bigint, byteLen: number): Uint8Array {
    let h = x.toString(16);
    if (h.length > byteLen * 2) throw new Error("overflow");
    if (h.length % 2) h = "0" + h;
    if (h.length < byteLen * 2) h = "0".repeat(byteLen * 2 - h.length) + h;
    return hexToBytesEven(h);
}

async function hkdfSha256(ikm: Uint8Array, salt: Uint8Array, info: Uint8Array, length = 32): Promise<Uint8Array> {
    const key = await crypto.subtle.importKey('raw', toArrayBufferStrict(ikm), 'HKDF', false, ['deriveBits']);
    // @ts-ignore
    const bits = await crypto.subtle.deriveBits({ name: 'HKDF', hash: 'SHA-256', salt, info },
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
    // @ts-ignore
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
