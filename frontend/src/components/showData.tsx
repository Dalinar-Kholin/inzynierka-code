import React, { useEffect, useMemo, useState } from "react";
import { sha256Hex } from "../helpers/pedersonCommitments.ts";
import { sha256 } from "@noble/hashes/sha2";
import {candidateMapping} from "../helpers/candidateMapping.ts";

type InnerMapping = Record<string, number>; // np. { "0": 3, "1": 1, "2": 2, "3": 0 }
type OuterMapping = Map<string, InnerMapping>;

export type VotingPack = {
    authSerial: string;
    lockCode: string;
    lockCodeCommitment: string;
    mapping: OuterMapping;
};

type Props = {
    pack: VotingPack;
    title?: string;
};

function shortenMiddle(s: string, head = 14, tail = 14) {
    if (!s) return "";
    if (s.length <= head + tail + 3) return s;
    return `${s.slice(0, head)}…${s.slice(-tail)}`;
}

async function copyToClipboard(text: string) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}

function Row({
    label,
    value,
    mono = false,
    wrap = false,
}: {
    label: string;
    value: string;
    mono?: boolean;
    wrap?: boolean;
}) {
    const [copied, setCopied] = useState(false);

    return (
        <div style={styles.row}>
            <div style={styles.label}>{label}</div>
            <div style={{ ...styles.value, ...(mono ? styles.mono : {}), ...(wrap ? styles.wrap : {}) }}>
                {value}
            </div>
            <button
                type="button"
                style={styles.copyBtn}
                onClick={async () => {
                    const ok = await copyToClipboard(value);
                    setCopied(ok);
                    setTimeout(() => setCopied(false), 900);
                }}
            >
                {copied ? "Skopiowano" : "Kopiuj"}
            </button>
        </div>
    );
}

export function VotingPackCard({pack, title = "Voting package"}: Props) {
    const [showCommitmentFull, setShowCommitmentFull] = useState(false);
    const [commitmentShown, setCommitmentShown] = useState("")

    const mappingNormalized = useMemo(() => {
        const out: Array<{
            serial: string;
            entries: Array<{ fromIdx: number; toIdx: number }>;
        }> = [];

        if (!pack.mapping || pack.mapping.size === 0) return out;

        for (const [serial, inner] of pack.mapping.entries()) {
            // inner jest obiektem { "0": 3, "1": 1, ... }
            const entries = Object.entries(inner)
                .map(([k, v]) => ({fromIdx: Number(k), toIdx: Number(v)}))
                .filter(e => Number.isFinite(e.fromIdx) && Number.isFinite(e.toIdx))
                .sort((a, b) => a.fromIdx - b.fromIdx);

            out.push({serial, entries});
        }

        // stabilnie sortuj po serialu (opcjonalnie)
        out.sort((a, b) => a.serial.localeCompare(b.serial));
        return out;
    }, [pack.mapping]);

    useEffect(() => {
        (async () => {
            const h1 = sha256(pack.lockCodeCommitment);
            const h2 = await sha256Hex(h1);
            setCommitmentShown(h2);
        })();
    }, [pack.lockCodeCommitment]);

    return (
        <div style={styles.card}>
            <div style={styles.header}>
                <div style={styles.title}>{title}</div>
                <div style={styles.badge}>authSerial: {shortenMiddle(pack.authSerial, 8, 8)}</div>
            </div>

            <div style={styles.section}>
                <Row label="authSerial" value={pack.authSerial} mono/>
                <Row label="lockCode" value={pack.lockCode} mono/>
                <div style={{...styles.row, alignItems: "flex-start"}}>
                    <div style={styles.label}>lockCodeCommitment</div>
                    <div style={{...styles.value, ...styles.mono, ...styles.wrap}}>{commitmentShown}</div>
                    <div style={styles.commitmentBtns}>
                        <button
                            type="button"
                            style={styles.smallBtn}
                            onClick={() => setShowCommitmentFull(s => !s)}
                        >
                            {showCommitmentFull ? "Zwiń" : "Pokaż całość"}
                        </button>
                        <button
                            type="button"
                            style={styles.smallBtn}
                            onClick={async () => {
                                await copyToClipboard(pack.lockCodeCommitment);
                            }}
                        >
                            Kopiuj
                        </button>
                    </div>
                </div>
            </div>

            <div style={styles.section}>
                <div style={styles.sectionTitle}>Mapping (SGX)</div>

                {mappingNormalized.length === 0 ? (
                    <div style={styles.empty}>Brak danych (mapping пустy)</div>
                ) : (
                    <div style={styles.mappingGrid}>
                        {mappingNormalized.map(block => (
                            <div key={block.serial} style={styles.mappingCard}>
                                <div style={styles.mappingHeader}>
                                    <div style={styles.mappingSerial}>{block.serial}</div>
                                    <button
                                        type="button"
                                        style={styles.smallBtn}
                                        onClick={async () => {
                                            await copyToClipboard(block.serial);
                                        }}
                                    >
                                        Kopiuj serial
                                    </button>
                                </div>

                                <div style={styles.table}>
                                    <div style={styles.thead}>
                                        <div>from</div>
                                        <div>to</div>
                                    </div>
                                    {block.entries.map(e => (
                                        <div key={`${block.serial}:${e.fromIdx}`} style={styles.tr}>
                                            <div style={styles.tdMono}>{e.fromIdx}</div>
                                            <div style={styles.tdMono}>{candidateMapping[e.toIdx]}</div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}

const styles: Record<string, React.CSSProperties> = {
    card: {
        border: "1px solid #e5e7eb",
        borderRadius: 16,
        padding: 16,
        boxShadow: "0 1px 6px rgba(0,0,0,0.06)",
        maxWidth: 900,
    },
    header: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
        marginBottom: 12,
    },
    title: { fontSize: 18, fontWeight: 700 },
    badge: {
        fontSize: 12,
        padding: "6px 10px",
        borderRadius: 999,
        border: "1px solid #e5e7eb",
        fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
    },
    section: {
        padding: 12,
        borderRadius: 12,
        border: "1px solid #e5e7eb",
        marginTop: 12,
    },
    sectionTitle: { fontSize: 14, fontWeight: 700, marginBottom: 8 },
    row: {
        display: "grid",
        gridTemplateColumns: "160px 1fr 90px",
        gap: 10,
        alignItems: "center",
        padding: "8px 0",
        borderBottom: "1px dashed #e5e7eb",
    },
    label: { fontSize: 13 },
    value: { fontSize: 13 },
    mono: { fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace" },
    wrap: { wordBreak: "break-all" },
    copyBtn: {
        border: "1px solid #e5e7eb",
        borderRadius: 10,
        padding: "6px 10px",
        cursor: "pointer",
        fontSize: 12,
    },
    smallBtn: {
        border: "1px solid #e5e7eb",
        borderRadius: 10,
        padding: "6px 10px",
        cursor: "pointer",
        fontSize: 12,
        whiteSpace: "nowrap",
    },
    commitmentBtns: {
        display: "flex",
        flexDirection: "column",
        gap: 8,
        alignItems: "stretch",
    },
    empty: { fontSize: 13, color: "#6b7280", padding: "6px 2px" },
    mappingGrid: {
        display: "grid",
        gridTemplateColumns: "repeat(auto-fit, minmax(320px, 1fr))",
        gap: 12,
    },
    mappingCard: {
        border: "1px solid #e5e7eb",
        borderRadius: 12,
        padding: 12,
    },
    mappingHeader: {
        display: "flex",
        justifyContent: "space-between",
        gap: 10,
        alignItems: "center",
        marginBottom: 10,
    },
    mappingSerial: {
        fontSize: 12,
        fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
        wordBreak: "break-all",
    },
    table: { display: "grid", gap: 6 },
    thead: {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: 8,
        fontSize: 12,
        fontWeight: 700,
    },
    tr: {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: 8,
        padding: "6px 8px",
        border: "1px solid #f3f4f6",
        borderRadius: 10,
    },
    tdMono: {
        fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
        fontSize: 13,
    },
};
