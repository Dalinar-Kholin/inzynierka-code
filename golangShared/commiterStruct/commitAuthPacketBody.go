package commiterStruct

type CommitAuthPacketBody struct {
	AuthSerial string `json:"authSerial"` // uuid in bytes in base64
	Data       string `json:"data"`
}

type CommitSignKeyBody struct {
	Key string `json:"key"`
}
