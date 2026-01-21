import {consts} from "../const.ts";


interface IGetVoteCode {
    permCode: string;
}

interface IGetVoteCodeResponse{
    voteCodes: string[];
    authSerial: string;
}


export default async function getVoteCode({permCode} : IGetVoteCode) {
    const response = await fetch(consts.SGX_URL + '/voter/codes?perm=' + permCode , {
        method: 'GET',
    })
    const data = await response.json()
    if (!response.ok) {
        throw new Error(data.error);
    }
    return data as IGetVoteCodeResponse[];
}