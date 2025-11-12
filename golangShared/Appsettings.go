package golangShared

const (
	CommiterPort = 8080
	VotingPort   = 8081
	SignerPort   = 8082

	GetVotingPackEndpoint = "/getVotingPack"

	GetAuthCodeInitEndpoint = "/getAuthCodeInit"
	GetAuthCodeEndpoint     = "/getAuthCode"
	AcceptVoteEndpoint      = "/acceptVote"

	AddCommitPackEndpoint = "/addAuthPack"

	FinalCommitEndpoint = "/finalCommit"

	SignEndpoint = "/sign"
)
