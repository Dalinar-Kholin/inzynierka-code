import './App.css'

import { Buffer } from 'buffer';

import { Button } from "@mui/material"
import { useNavigate } from "react-router-dom";

import * as ed from "@noble/ed25519";
import { sha256, sha512 } from "@noble/hashes/sha2";

if (!(ed as any).hashes) {
    (ed as any).hashes = {};
}
(ed as any).hashes.sha512 = sha512;

if (!(window as any).Buffer) {
    (window as any).Buffer = Buffer;
}

import useGetServerPubKey from "./hooks/useGetServerPubKey.ts";

function App() {
    const navigate = useNavigate();
    const { pubKey } = useGetServerPubKey()

    if (pubKey === "") {
        return <>loading</>;
    }

    return (
        <>
            <Button onClick={() => navigate("/helperDeviceView")}>helper device</Button>
            <Button onClick={() => navigate("/votingDeviceView")}>voter device</Button>
        </>
    )
}

export default App