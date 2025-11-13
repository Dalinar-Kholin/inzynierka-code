import {consts} from "../const.ts";


interface IGetVoteCode {
    voteSerial: string;
}

export default function getVoteCode({voteSerial} : IGetVoteCode) {
    return fetch(consts.API_URL + '/getVoteCodes', {
        method: 'POST',
        body: JSON.stringify({
            voteSerial: voteSerial
        }),
    }).then(response => {
        if (!response.ok) {
            console.log("essa")
        }
        return response.json();
    })
}