import {Route, Routes} from 'react-router-dom';
import App from "../App.tsx";
import {ConnectionProvider} from "@solana/wallet-adapter-react";
import "@solana/wallet-adapter-react-ui/styles.css";
import ConnectWallet from "./ConnectWallet.tsx";
import VotingDeviceView from "./voting/VotingDeviceView.tsx";
import HelperDeviceView from "./voting/HelperDeviceView.tsx";

const endpoint = "http://127.0.0.1:8899";


export default function RouterSwitcher() {
    return (
        <ConnectionProvider endpoint={endpoint}>
            <Routes>
                <Route path={"/"} element={<App/>}/>
                <Route path={"/connectWallet"} element={<ConnectWallet/>}/>
                <Route path={"/votingDeviceView"} element={<VotingDeviceView/>}/>
                <Route path={"/helperDeviceView"} element={<HelperDeviceView/>}/>
            </Routes>
        </ConnectionProvider>
    )
}