import type {Ballot} from "./context.tsx";

type BallotAction = {
    type: "SET_VOTE_SERIAL",
    payload : {voteSerial: string}
} | {
    type: "SET_AUTH_SERIAL",
    payload : { authSerial: string }
}| {
    type: "SET_ACK_CODE",
    payload : { code: string }
}| {
    type: "SET_VOTE_CODE",
    payload : { code: string[] }
} | {
    type: "SET_AUTH_CODE",
    payload : { code: string }
}


export const ballotReducer = (state: Ballot, action: BallotAction): Ballot => {
    switch (action.type) {
        case "SET_VOTE_SERIAL":
            return { ...state, VOTE_SERIAL: action.payload.voteSerial };
        case "SET_AUTH_SERIAL":
            return { ...state, AUTH_SERIAL: action.payload.authSerial };
        case "SET_ACK_CODE":
            return { ...state, ACK_CODE: action.payload.code };
        case "SET_VOTE_CODE":
            return { ...state, VOTE_CODES: action.payload.code };
        case "SET_AUTH_CODE":
            return { ...state, AUTH_CODE: action.payload.code };
        default:
            return state;
    }
};
