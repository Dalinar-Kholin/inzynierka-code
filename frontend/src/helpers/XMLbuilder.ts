class Vote {
    public VoteSerial: string;

    public VoteCode: string;

    public AuthSerial: string;

    public AuthCode: string;

    public ServerSign: number[];


    constructor(
        VoteSerial: string,
        VoteCode: string,
        AuthSerial: string,
        AuthCode: string,
        ServerSign: number[],
    ) {
        this.ServerSign = ServerSign;
        this.AuthCode = AuthCode;
        this.AuthSerial = AuthSerial;
        this.VoteCode = VoteCode;
        this.VoteSerial = VoteSerial;
    }
}

export default Vote

export function serializeVoteToXML(vote: Vote): string {
    const doc = document.implementation.createDocument("", "vote", null);
    const root = doc.documentElement;

    let voterEl = doc.createElement("VoteSerial");
    voterEl.textContent = vote.VoteSerial;
    root.appendChild(voterEl);

    voterEl = doc.createElement("VoteCode");
    voterEl.textContent = vote.VoteCode;
    root.appendChild(voterEl);

    voterEl = doc.createElement("AuthSerial");
    voterEl.textContent = vote.AuthSerial;
    root.appendChild(voterEl);

    voterEl = doc.createElement("AuthCode");
    voterEl.textContent = vote.AuthCode;
    root.appendChild(voterEl);

    voterEl = doc.createElement("ServerSign");
    voterEl.textContent = vote.ServerSign.toString();
    root.appendChild(voterEl);

    const serializer = new XMLSerializer();
    return serializer.serializeToString(doc);
}