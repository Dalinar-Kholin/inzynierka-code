import './App.css'

import { Buffer } from 'buffer';

import { Button } from "@mui/material"
import {useNavigate} from "react-router-dom";
import ConnectWallet from "./components/ConnectWallet.tsx";

if (!(window as any).Buffer) {
    (window as any).Buffer = Buffer;
}

function App() {
    const navigate = useNavigate();

    return (
        <>
            <ConnectWallet/>
            <Button onClick={()=> navigate("/helperDeviceView")}>helper device</Button>
            <Button onClick={()=> navigate("/votingDeviceView")}>voter device</Button>
        </>
    )
}

export default App
