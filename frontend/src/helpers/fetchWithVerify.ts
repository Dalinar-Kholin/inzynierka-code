import {verifyEd25519} from "../components/getVoteStatus.tsx";

interface ServerResponse<T> {
    body: T;
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

export default async function fetchWithAuth<T>(url: string, options: RequestInit, verifyKey: string): Promise<T | ServerError | BadSignError> {
    const res = await fetch(url, options);

    const json: ServerResponse<T> | ServerError = await res.json()

    if (!res.ok) {
        return json as ServerError;
    }

    const serverResponse = json as ServerResponse<T>;
    
    if (!await verifyEd25519(verifyKey, JSON.stringify(serverResponse.body), serverResponse.sign, "base64")){
        StoreFailure.push(serverResponse)
        return {badSign: serverResponse.sign} as BadSignError
    }
    StoreSuccess.push(serverResponse)
    return serverResponse.body;
}


export function IsServerError(u: unknown): u is ServerError {
    return typeof u === 'object' && u !== null && 'error' in u;
}
export function IsBadSignError(u: unknown): u is BadSignError {
    return typeof u === 'object' && u !== null && 'badSign' in u;
}