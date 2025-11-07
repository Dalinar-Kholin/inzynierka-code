package merkeleTree

import (
	"crypto/sha256"
	"encoding/hex"
	"errors"
	"fmt"
)

type MerkleTree struct {
	levels [][][32]byte // levels[0] are leaf hashes, levels[len-1][0] is the root
}

func NewMerkleTree(leaves [][]byte) (*MerkleTree, error) {
	if len(leaves) == 0 {
		return &MerkleTree{levels: nil}, nil
	}

	lvl := make([][32]byte, len(leaves))
	for i, d := range leaves {
		lvl[i] = leafHash(d)
	}

	levels := [][][32]byte{lvl}
	for len(lvl) > 1 {
		var next [][32]byte
		for i := 0; i < len(lvl); i += 2 {
			left := lvl[i]
			var right [32]byte
			if i+1 < len(lvl) {
				right = lvl[i+1]
			} else {
				// duplicate the last node to handle odd count
				right = lvl[i]
			}
			next = append(next, parentHash(left, right))
		}
		levels = append(levels, next)
		lvl = next
	}

	return &MerkleTree{levels: levels}, nil
}

// Root returns the Merkle root. For an empty tree it returns 32 zero bytes.
func (t *MerkleTree) Root() [32]byte {
	if t == nil || len(t.levels) == 0 {
		return [32]byte{}
	}
	return t.levels[len(t.levels)-1][0]
}

func (t *MerkleTree) LeafCount() int {
	if t == nil || len(t.levels) == 0 {
		return 0
	}
	return len(t.levels[0])
}

type Proof struct {
	Siblings [][32]byte // sibling hashes from leaf level upwards
	IsRight  []bool     // at each height: true if current node is a right child (sibling is on the left)
	Index    int        // original leaf index
}

// GenerateProof returns a Merkle inclusion proof for the leaf at index i.
func (t *MerkleTree) GenerateProof(i int) (Proof, error) {
	if t == nil || len(t.levels) == 0 {
		return Proof{}, errors.New("empty tree")
	}
	if i < 0 || i >= len(t.levels[0]) {
		return Proof{}, fmt.Errorf("index %d out of range [0,%d)", i, len(t.levels[0]))
	}

	idx := i
	var sibs [][32]byte
	var rights []bool
	for h := 0; h < len(t.levels)-1; h++ {
		lvl := t.levels[h]
		var sib [32]byte
		if idx%2 == 0 { // left child
			if idx+1 < len(lvl) {
				sib = lvl[idx+1]
			} else {
				// duplicate if missing
				sib = lvl[idx]
			}
			rights = append(rights, false)
		} else { // right child
			sib = lvl[idx-1]
			rights = append(rights, true)
		}
		sibs = append(sibs, sib)
		idx /= 2
	}
	return Proof{Siblings: sibs, IsRight: rights, Index: i}, nil
}

func VerifyProof(data []byte, p Proof, root [32]byte) bool {
	h := leafHash(data)
	idx := p.Index
	for j := 0; j < len(p.Siblings); j++ {
		s := p.Siblings[j]
		if idx%2 == 0 { // left child
			h = parentHash(h, s)
		} else { // right child
			h = parentHash(s, h)
		}
		idx /= 2
	}
	return h == root
}

func leafHash(data []byte) [32]byte {
	b := make([]byte, 1+len(data))
	b[0] = 0x00
	copy(b[1:], data)
	return sha256.Sum256(b)
}

func parentHash(left, right [32]byte) [32]byte {
	b := make([]byte, 1+32+32)
	b[0] = 0x01
	copy(b[1:1+32], left[:])
	copy(b[1+32:], right[:])
	return sha256.Sum256(b)
}
func hex32(x [32]byte) string { return hex.EncodeToString(x[:]) }
