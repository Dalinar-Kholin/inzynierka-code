package golangShared

const (
	CommiterPort = 8080
	VotingPort   = 8081
	SignerPort   = 8082
	VerifierPort = 8083

	GetVotingPackEndpoint   = "/getVotingPack"
	GetVoteCodesEndpoint    = "/getVoteCodes"
	GetAuthCodeInitEndpoint = "/getAuthCodeInit"
	GetAuthCodeEndpoint     = "/getAuthCode"
	AcceptVoteEndpoint      = "/acceptVote"

	CommitVoteEndpoint = "/commitVote"

	AddCommitPackEndpoint = "/addAuthPack"

	FinalCommitEndpoint = "/finalCommit"

	SignEndpoint = "/sign"
)
