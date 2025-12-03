import {useEffect, useState} from "react";
import {useAnchor} from "./useAnchor.ts";

let cachedPubKey: string | null = null;
let fetchingPromise: Promise<string> | null = null;

export default function useGetServerPubKey() {
    const [pubKey, setPubKey] = useState<string>("")
    const { getProgram } = useAnchor()

    useEffect(() => {
        if (cachedPubKey) {
            setPubKey(cachedPubKey);
            return;
        }

        if (!fetchingPromise) {
            fetchingPromise = (async () => {
                const result = await getProgram().account.signKey.all();
                const key = new TextDecoder("utf-8")
                    .decode(new Uint8Array(result[0].account.key))
                    .replace("-----BEGIN PUBLIC KEY-----", "")
                    .replace("-----END PUBLIC KEY-----", "")
                    .trim();
                cachedPubKey = key;
                return key;
            })();
        }

        fetchingPromise
            .then(key => setPubKey(key))
            .catch(e => console.log(e));
    }, [getProgram]);

    return { pubKey };
}