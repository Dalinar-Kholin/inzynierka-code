import {verifyEd25519} from "../components/getVoteStatus.tsx";


interface Body<T, E>{
    userRequest: E
    content: T
}

interface ServerResponse<T, E> {
    body: Body<T, E>;
    sign: string
}

interface ServerError{
    error: string;
}

interface BadSignError{
    badSign: string
}


const StoreSuccess = []
const StoreFailure = []

export default async function fetchWithAuth<T, E>(url: string, options: RequestInit, verifyKey: string, body: E): Promise<T | ServerError | BadSignError> {
    options.body = JSON.stringify(body)
    const res = await fetch(url, options);
    const json: ServerResponse<T, E> | ServerError = await res.json()

    if (!res.ok) {
        return json as ServerError;
    }

    const serverResponse = json as ServerResponse<T, E>;

    if (!shallowEqual(body, serverResponse.body.userRequest)){
        return {badSign: `server sign improper data ${serverResponse.sign}`} as BadSignError
    }

    if (!await verifyEd25519(verifyKey, new TextEncoder().encode(JSON.stringify(serverResponse.body)), serverResponse.sign, "base64")){
        StoreFailure.push(serverResponse)
        return {badSign: serverResponse.sign} as BadSignError
    }
    StoreSuccess.push(serverResponse)
    return serverResponse.body.content;
}


export function IsServerError(u: unknown): u is ServerError {
    return typeof u === 'object' && u !== null && 'error' in u;
}
export function IsBadSignError(u: unknown): u is BadSignError {
    return typeof u === 'object' && u !== null && 'badSign' in u;
}



function shallowEqual<T>(a: T, b: T): boolean {
    let aKeys: (keyof T)[];
    // @ts-ignore
    aKeys = Object.keys(a) as (keyof T)[];
    let bKeys: (keyof T)[];
    // @ts-ignore
    bKeys = Object.keys(b) as (keyof T)[];

    if (aKeys.length !== bKeys.length) return false;

    return aKeys.every((k) => a[k] === b[k]);
}