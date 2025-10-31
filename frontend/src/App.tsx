import { useNavigate } from "react-router-dom"
import './App.css'
import {Button} from "@mui/material"
import {WalletMultiButton} from "@solana/wallet-adapter-react-ui";

import { Buffer } from 'buffer';

if (!(window as any).Buffer) {
    (window as any).Buffer = Buffer;
}


function App() {
    const navigate = useNavigate()
    return (
    <>


        <WalletMultiButton />
        ????
        <Button onClick={()=>navigate("ballots")}>naviguj do ballots</Button>
        <Button onClick={()=>navigate("commits")}>naviguj do commits</Button>
        essa
    </>
  )
}

export default App
