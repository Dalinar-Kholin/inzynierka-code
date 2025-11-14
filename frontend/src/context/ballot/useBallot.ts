import {useContext} from "react";
import {BallotContext} from "./context.tsx";

export default function useBallot(){
    const context = useContext(BallotContext)
    if (!context){
        throw new Error("where context?")
    }
    return context
}