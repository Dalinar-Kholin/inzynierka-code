import {Route, Routes} from 'react-router-dom';
import App from "../App.tsx";
import GetBallots from "./getBallots.tsx";
import {ConnectionProvider, WalletProvider} from "@solana/wallet-adapter-react";
import {WalletModalProvider} from "@solana/wallet-adapter-react-ui";
import {BraveWalletAdapter} from '@solana/wallet-adapter-brave';
import {PhantomWalletAdapter} from '@solana/wallet-adapter-phantom';
import "@solana/wallet-adapter-react-ui/styles.css";
import {CommitmentComp} from "./CommitmentComp.tsx";
import SendVote from "./sendVote.tsx";
import ConnectWallet from "./ConnectWallet.tsx";
import Vote from "./Vote.tsx";

const endpoint = "http://127.0.0.1:8899";
const wallets = [
    new BraveWalletAdapter(),
    new PhantomWalletAdapter(),
];

export default function RouterSwitcher() {
    return (
            <ConnectionProvider endpoint={endpoint}>
                <WalletProvider wallets={wallets} autoConnect>
                    <WalletModalProvider>
                        <Routes>
                            <Route path={"/"} element={<App/>}/>
                            <Route path={"/ballots"} element={<GetBallots/>}/>
                            <Route path={"/commits"} element={<CommitmentComp/>}/>
                            <Route path={"/sendVote"} element={<SendVote/>}/>
                            <Route path={"/connectWallet"} element={<ConnectWallet/>}/>
                            <Route path={"/vote"} element={<Vote/>}/>
                        </Routes>
                    </WalletModalProvider>
                </WalletProvider>
            </ConnectionProvider>
    )
}