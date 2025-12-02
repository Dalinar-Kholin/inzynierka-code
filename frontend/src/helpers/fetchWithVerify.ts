import {verifyEd25519} from "../components/getVoteStatus.tsx";
import * as ed from "@noble/ed25519";
import useGetFrontendKey from "../hooks/useGetFrontendKey.ts";
import useGetServerPubKey from "../hooks/useGetServerPubKey.ts";

interface Body<T, E>{userRequest: E; content: T}

interface ServerResponse<T, E> {body: Body<T, E>;sign: string}

export interface ServerError{error: string;}

export interface BadSignError{badSign: string}

interface ServerRequest<E>{body: E; sign: string}

const StoreSuccess = []
const StoreFailure = []

export default function useFetchWithVerify(){
    const {publicKey, privateKey} = useGetFrontendKey()
    const { pubKey } = useGetServerPubKey()

    async function wrappedFetchWithVerify<T, E>(url: string, options: RequestInit, body: E): Promise<T | ServerError | BadSignError>{
        return await fetchWithAuth(url, options, pubKey, body, publicKey, privateKey)
    }

    return {
        fetchWithAuth : wrappedFetchWithVerify
    }
}

async function fetchWithAuth<T, E>(url: string, options: RequestInit, verifyKey: string, body: E, frontendPublicKey: string, frontendPrivateKey: string): Promise<T | ServerError | BadSignError> {

    const jsonedBody = JSON.stringify(body)

    const signed = signMessageEd25519(jsonedBody, frontendPrivateKey)

    const newBody: ServerRequest<E> = {
        body: body,
        sign: signed,
    }

    options.body = JSON.stringify(newBody)
    const res = await fetch(url, options);
    const json: ServerResponse<T, ServerRequest<E>> | ServerError = await res.json()

    if (!res.ok) {
        return json as ServerError;
    }

    const serverResponse = json as ServerResponse<T, ServerRequest<E>>;

    if (!deepEqual(newBody, serverResponse.body.userRequest)) {
        console.log(newBody)
        console.log(serverResponse.body.userRequest)
        return {badSign: `server sign improper data ${serverResponse.sign}`} as BadSignError
    } // sprawdzamy czy ciało jest takie samo

    if (!verifyMessageEd25519(jsonedBody, serverResponse.body.userRequest.sign, frontendPublicKey)) {
        console.log("how da fuck")
        return {badSign: `server sign improper data`} as BadSignError
    } // sprawdzamy czy podpis się zgadza

    if (!await verifyEd25519(verifyKey, new TextEncoder().encode(JSON.stringify(serverResponse.body)), serverResponse.sign, "base64")){
        console.log(JSON.stringify(serverResponse.body))
        StoreFailure.push(serverResponse)
        return {badSign: serverResponse.sign} as BadSignError
    }

    StoreSuccess.push(serverResponse)
    return serverResponse.body.content;
}

export function deepEqual(a: any, b: any): boolean {
    if (a === b) return true;

    if (typeof a === "number" && typeof b === "number") {
        return Number.isNaN(a) && Number.isNaN(b);
    }

    if (
        a === null || b === null ||
        typeof a !== "object" ||
        typeof b !== "object"
    ) {
        return false;
    }

    const isArrayA = Array.isArray(a);
    const isArrayB = Array.isArray(b);
    if (isArrayA || isArrayB) {
        if (!isArrayA || !isArrayB) return false;
        if (a.length !== b.length) return false;
        for (let i = 0; i < a.length; i++) {
            if (!deepEqual(a[i], b[i])) return false;
        }
        return true;
    }

    const aKeys = Object.keys(a);
    const bKeys = Object.keys(b);

    for (const key of aKeys) {
        const aVal = a[key];

        const bHasKey = Object.prototype.hasOwnProperty.call(b, key);
        if (!bHasKey) {
            // Jeżeli w a jest null, a w b brak klucza → OK
            if (aVal === null) continue;
            return false;
        }

        const bVal = b[key];
        if (!deepEqual(aVal, bVal)) {
            return false;
        }
    }

    for (const key of bKeys) {
        const bVal = b[key];

        const aHasKey = Object.prototype.hasOwnProperty.call(a, key);
        if (!aHasKey) {
            if (bVal === null) continue;
            return false;
        }
    }

    return true;
}

function pkcs8ToRawEd25519Private(pkcs8: Uint8Array): Uint8Array {
    if (pkcs8.length < 32) throw new Error("PKCS#8 too short");
    return pkcs8.slice(pkcs8.length - 32);
}

function spkiToRawEd25519Public(spki: Uint8Array): Uint8Array {
    if (spki.length < 32) throw new Error("SPKI too short");
    return spki.slice(spki.length - 32);
}

function base64ToBytes(base64: string): Uint8Array {
    const binary = atob(base64);
    const out = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) out[i] = binary.charCodeAt(i);
    return out;
}

function bytesToBase64(bytes: Uint8Array): string {
    let binary = "";
    for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
    return btoa(binary);
}

// SIGN – tu NIE MA potrzeby async/await
export function signMessageEd25519(
    message: string,
    privateKeyDerBase64: string
): string {
    const msgBytes = new TextEncoder().encode(message);

    const pkcs8Bytes = base64ToBytes(privateKeyDerBase64);
    const privRaw = pkcs8ToRawEd25519Private(pkcs8Bytes); // 32 bytes

    const signature = ed.sign(msgBytes, privRaw); // boolean? NIE, tu jest Uint8Array

    return bytesToBase64(signature);
}

// VERIFY – również synchroniczne
export function verifyMessageEd25519(
    message: string,
    signatureBase64: string,
    publicKeyDerBase64: string
): boolean {
    const msgBytes = new TextEncoder().encode(message);
    const sigBytes = base64ToBytes(signatureBase64);

    const spkiBytes = base64ToBytes(publicKeyDerBase64);
    const pubRaw = spkiToRawEd25519Public(spkiBytes); // 32 bytes

    return ed.verify(sigBytes, msgBytes, pubRaw); // <- boolean, bez await
}

export function IsServerError(u: unknown): u is ServerError {
    return typeof u === 'object' && u !== null && 'error' in u;
}
export function IsBadSignError(u: unknown): u is BadSignError {
    return typeof u === 'object' && u !== null && 'badSign' in u;
}