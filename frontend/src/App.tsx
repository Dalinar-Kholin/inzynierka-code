import { useNavigate } from "react-router-dom"
import './App.css'
import {Button} from "@mui/material"
import {WalletMultiButton} from "@solana/wallet-adapter-react-ui";
import { useWallet } from '@solana/wallet-adapter-react';

import { Buffer } from 'buffer';
import {useEffect, useState} from "react";

if (!(window as any).Buffer) {
    (window as any).Buffer = Buffer;
}


function App() {
    const navigate = useNavigate()
    const { publicKey } = useWallet();
    const [pubBase58, setPubBase58] = useState<string | null>(null);

    useEffect(() => {
        if (publicKey) {
            setPubBase58(publicKey.toBase58());
        } else {
            setPubBase58(null);
        }
    }, [publicKey]);


    return (
    <>
        {pubBase58 ?? ""}

        <WalletMultiButton />
        ????
        <Button onClick={()=>navigate("ballots")}>naviguj do ballots</Button>
        <Button onClick={()=>navigate("commits")}>naviguj do commits</Button>
        essa
    </>
  )
}

export default App
