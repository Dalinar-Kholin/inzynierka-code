package helper

import "github.com/klauspost/compress/zstd"

func CompressXMLZstd(data []byte) ([]byte, error) {
	enc, err := zstd.NewWriter(nil,
		zstd.WithEncoderLevel(zstd.SpeedBetterCompression),
	)
	if err != nil {
		return nil, err
	}
	defer enc.Close()

	return enc.EncodeAll(data, make([]byte, 0, len(data))), nil
}

func DecompressXMLZstd(comp []byte) ([]byte, error) {
	dec, err := zstd.NewReader(nil)
	if err != nil {
		return nil, err
	}
	defer dec.Close()

	return dec.DecodeAll(comp, nil)
}
