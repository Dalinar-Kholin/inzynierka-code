import {createContext, useReducer} from "react";
import {ballotReducer} from "./reducer.ts";

export class Ballot{
    VOTE_SERIAL : string;
    AUTH_SERIAL : string;
    ACK_CODE : string;
    VOTE_CODES : string[];
    AUTH_CODE: string;
    SELECTED_CODE: string;
    VOTER_SIGN : string;
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
        this.SELECTED_CODE = ""
        this.VOTER_SIGN = ""
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
    setSelectedCode: (s : string) => void
    setVoterSign: (s : string) => void
}>(
    {
        ballot : initialState,
        setVoteSerial: ()=> {},
        setAuthSerial: () => {},
        setAckCode: () => {},
        setAuthCode: ()=>{},
        setVoteCodes: () => {},
        setSelectedCode: () => {},
        setVoterSign: () => {},
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
        dispatch({type: "SET_ACK_CODES", payload: {code: ackCode}})
    }

    function setAuthCode(authCode: string){
        dispatch({type: "SET_AUTH_CODE", payload: {code: authCode}})
    }

    function setVoteCodes(voteCodes: string[]){
        dispatch({type: "SET_VOTE_CODES", payload: {code: voteCodes}})
    }

    function setSelectedCode(voteCode: string){
        dispatch({type: "SET_VOTE_CODE", payload: {code: voteCode}})
    }

    function setVoterSign(sign: string){
        dispatch({type: "SET_VOTER_SIGN", payload: {code: sign}})
    }

    return(
        <BallotContext.Provider value={{ballot, setVoteSerial, setAuthSerial, setAckCode, setAuthCode, setVoteCodes, setSelectedCode, setVoterSign}}>
            {children}
        </BallotContext.Provider>
    )
}