import {useEffect, useState} from "react";
import {useAnchor} from "./useAnchor.ts";

export default function useGetServerPubKey() {
    const [pubKey, setPubKey] = useState<string>("")
    const {getProgram} = useAnchor()

    useEffect(() => { // load Key
        const fetch = async () => {
            const pubKey = (new TextDecoder("utf-8")).decode(new Uint8Array((await getProgram().account.signKey.all())[0].account.key)).replace("-----BEGIN PUBLIC KEY-----", "").replace("-----END PUBLIC KEY-----", "").trim()
            setPubKey(pubKey)
        }
        fetch().then();
    }, []);


    return {
        pubKey,
    };
}