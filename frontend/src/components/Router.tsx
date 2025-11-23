import {Route, Routes} from 'react-router-dom';
import App from "../App.tsx";
import {ConnectionProvider, WalletProvider} from "@solana/wallet-adapter-react";
import "@solana/wallet-adapter-react-ui/styles.css";
import ConnectWallet from "./ConnectWallet.tsx";
import {WalletModalProvider} from "@solana/wallet-adapter-react-ui";
import {BraveWalletAdapter} from "@solana/wallet-adapter-brave";
import {PhantomWalletAdapter} from "@solana/wallet-adapter-phantom";
import VotingDeviceView from "./voting/VotingDeviceView.tsx";
import HelperDeviceView from "./voting/HelperDeviceView.tsx";

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
                            <Route path={"/connectWallet"} element={<ConnectWallet/>}/>
                            <Route path={"/votingDeviceView"} element={<VotingDeviceView/>}/>
                            <Route path={"/helperDeviceView"} element={<HelperDeviceView/>}/>
                        </Routes>
                    </WalletModalProvider>
                </WalletProvider>
            </ConnectionProvider>
    )
}