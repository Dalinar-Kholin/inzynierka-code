package golangShared

const (
	CommiterPort = 8080
	VotingPort   = 8081
	SignerPort   = 8082
	VerifierPort = 8083
	StorerPort   = 8084
	ProxyPort    = 8085
	SGXPort      = 8086

	GetVotingPackEndpoint   = "/getVotingPack"
	GetVoteCodesEndpoint    = "/getVoteCodes"
	GetAuthCodeInitEndpoint = "/getAuthCodeInit"
	GetAuthCodeEndpoint     = "/getAuthCode"
	AcceptVoteEndpoint      = "/acceptVote"

	StorerEndpoint = "/Upload"
	//CommitVoteEndpoint = "/commitVote"

	AddCommitPackEndpoint = "/addAuthPack"

	FinalCommitEndpoint       = "/finalCommit"
	CommitSignKeyEndpoint     = "/commitSignKey"
	CommitSingleValueEndpoint = "/commitSingleValue"
	UpdateVoteVectorEndpoint  = "/updateVoteVector"

	VerifySignKeyEndpoint = "/verifySignKey"

	SignEndpoint = "/sign"
)
