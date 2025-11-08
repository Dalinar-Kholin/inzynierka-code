import './App.css'
import {WalletMultiButton} from "@solana/wallet-adapter-react-ui";
import { useWallet } from '@solana/wallet-adapter-react';

import { Buffer } from 'buffer';
import {useEffect, useState} from "react";
import SendVote from "./components/sendVote.tsx";
import GetBallots from "./components/getBallots.tsx";

if (!(window as any).Buffer) {
    (window as any).Buffer = Buffer;
}

function App() {
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
            <WalletMultiButton/>
            <p></p>
            <SendVote/>
            <p></p>
            <GetBallots/>
            {pubBase58 ?? ""}

        </>
    )
}

export default App
