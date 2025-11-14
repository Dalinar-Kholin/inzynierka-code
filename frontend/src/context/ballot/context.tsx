import {createContext, useReducer} from "react";
import {ballotReducer} from "./reducer.ts";

export class Ballot{
    VOTE_SERIAL : string;
    AUTH_SERIAL : string;
    ACK_CODE : string;
    VOTE_CODES : string[];
    AUTH_CODE: string;
    constructor(VOTE_SERIAL : string,
                AUTH_SERIAL : string,
                ACK_CODE : string,
                VOTE_CODES : string[],
                AUTH_CODE: string
    ) {
        this.VOTE_SERIAL = VOTE_SERIAL
        this.AUTH_SERIAL = AUTH_SERIAL
        this.ACK_CODE = ACK_CODE
        this.VOTE_CODES = VOTE_CODES
        this.AUTH_CODE = AUTH_CODE
    }
}


const initialState = new Ballot("", "" ,"", [], "")

export const BallotContext = createContext<{
    ballot : Ballot
    setVoteSerial: (s : string) => void
    setAuthSerial: (s : string) => void
    setAckCode: (s : string) => void
    setVoteCodes: (s : string[]) => void
    setAuthCode: (s : string) => void
}>(
    {
        ballot : initialState,
        setVoteSerial: ()=> {},
        setAuthSerial: () => {},
        setAckCode: () => {},
        setAuthCode: ()=>{},
        setVoteCodes: () => {},
    }
)


export const BallotProvider = ({ children }: { children: React.ReactNode }) => {
    const [ballot, dispatch] = useReducer(ballotReducer,initialState)

    function setVoteSerial(voteSerial: string){
        dispatch({type: "SET_VOTE_SERIAL", payload: {voteSerial}})
    }

    function setAuthSerial(authSerial: string){
        dispatch({type: "SET_AUTH_SERIAL", payload: {authSerial}})
    }

    function setAckCode(ackCode: string){
        dispatch({type: "SET_ACK_CODE", payload: {code: ackCode}})
    }

    function setAuthCode(authCode: string){
        dispatch({type: "SET_AUTH_CODE", payload: {code: authCode}})
    }

    function setVoteCodes(voteCodes: string[]){
        dispatch({type: "SET_VOTE_CODE", payload: {code: voteCodes}})
    }

    return(
        <BallotContext.Provider value={{ballot, setVoteSerial, setAuthSerial, setAckCode, setAuthCode, setVoteCodes}}>
            {children}
        </BallotContext.Provider>
    )
}