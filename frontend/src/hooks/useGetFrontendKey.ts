import {useState} from "react";


export default function useGetFrontendKey(){
    const [publicKey, setPublicKey] = useState<string>("MCowBQYDK2VwAyEAtfPRnhwoDJQcinqjP9zbTI4zuV9GR3MVcLZSPww4BbM=")
    const [privateKey, setPrivateKey] = useState<string>("MC4CAQAwBQYDK2VwBCIEIAhAjfuwEVKEAmvlx7MelmOHQroCipATgkg5reYCcC3C")

    return {
        publicKey,
        privateKey,
        setPublicKey,
        setPrivateKey
    }
}