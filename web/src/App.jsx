import { useState } from "react";

// Maps the API's { "error": "..." } envelope status to a short row label.
const STATUS_LABELS = {
    400: "Invalid SSN",
    401: "Unauthorized",
    404: "Not found",
    422: "Incomplete",
    429: "Rate limited",
    500: "Server error",
};

async function lookupSsn(ssn, apiKey) {
    const response = await fetch(`/kyc-data/${encodeURIComponent(ssn)}`, {
        headers: { "X-Api-Key": apiKey },
    });

    let body = null;
    try {
        body = await response.json();
    } catch {
        body = null;
    }

    if (response.ok) {
        return { state: "ok", data: body };
    }

    const label = STATUS_LABELS[response.status] ?? `HTTP ${response.status}`;
    const message = body?.error ?? label;
    return { state: "error", httpStatus: response.status, label, message };
}

export default function App() {
    const [apiKey, setApiKey] = useState("");
    const [ssnInput, setSsnInput] = useState("");
    const [ssns, setSsns] = useState([]);
    const [results, setResults] = useState({});
    const [keyError, setKeyError] = useState("");
    const [busy, setBusy] = useState(false);

    function addSsn() {
        const value = ssnInput.trim();
        if (!value || ssns.includes(value)) {
            setSsnInput("");
            return;
        }
        setSsns((prev) => [...prev, value]);
        setSsnInput("");
    }

    function removeSsn(ssn) {
        setSsns((prev) => prev.filter((s) => s !== ssn));
        setResults((prev) => {
            const next = { ...prev };
            delete next[ssn];
            return next;
        });
    }

    async function lookupAll() {
        if (!apiKey.trim()) {
            setKeyError("Enter an API key before looking up SSNs.");
            return;
        }
        setKeyError("");
        setBusy(true);
        setResults(Object.fromEntries(ssns.map((s) => [s, { state: "loading" }])));

        await Promise.all(
            ssns.map(async (ssn) => {
                try {
                    const result = await lookupSsn(ssn, apiKey);
                    setResults((prev) => ({ ...prev, [ssn]: result }));
                } catch (err) {
                    setResults((prev) => ({
                        ...prev,
                        [ssn]: { state: "error", label: "Request failed", message: String(err) },
                    }));
                }
            })
        );

        setBusy(false);
    }

    return (
        <main className="app">
            <h1>KYC Client</h1>

            <section className="panel">
                <label className="field">
                    <span>API key</span>
                    <input
                        type="password"
                        value={apiKey}
                        onChange={(e) => setApiKey(e.target.value)}
                        placeholder="X-Api-Key"
                        autoComplete="off"
                    />
                </label>
                {keyError && <p className="error-text">{keyError}</p>}
            </section>

            <section className="panel">
                <div className="field-row">
                    <input
                        type="text"
                        value={ssnInput}
                        onChange={(e) => setSsnInput(e.target.value)}
                        onKeyDown={(e) => e.key === "Enter" && addSsn()}
                        placeholder="YYYYMMDD-NNNN"
                    />
                    <button type="button" onClick={addSsn}>
                        Add
                    </button>
                    <button
                        type="button"
                        onClick={lookupAll}
                        disabled={busy || ssns.length === 0}
                    >
                        {busy ? "Looking up…" : "Look up all"}
                    </button>
                </div>

                {ssns.length > 0 && (
                    <ul className="ssn-list">
                        {ssns.map((ssn) => (
                            <li key={ssn}>
                                <span>{ssn}</span>
                                <button type="button" onClick={() => removeSsn(ssn)}>
                                    ✕
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            {ssns.length > 0 && (
                <section className="panel">
                    <table className="results">
                        <thead>
                            <tr>
                                <th>SSN</th>
                                <th>Name</th>
                                <th>Address</th>
                                <th>Tax country</th>
                                <th>Phone</th>
                                <th>Email</th>
                                <th>Income</th>
                                <th>Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            {ssns.map((ssn) => (
                                <ResultRow key={ssn} ssn={ssn} result={results[ssn]} />
                            ))}
                        </tbody>
                    </table>
                </section>
            )}
        </main>
    );
}

function ResultRow({ ssn, result }) {
    if (!result) {
        return (
            <tr>
                <td>{ssn}</td>
                <td colSpan={6} className="muted">
                    —
                </td>
                <td className="muted">Not looked up</td>
            </tr>
        );
    }

    if (result.state === "loading") {
        return (
            <tr>
                <td>{ssn}</td>
                <td colSpan={6} className="muted">
                    …
                </td>
                <td className="muted">Loading</td>
            </tr>
        );
    }

    if (result.state === "error") {
        return (
            <tr>
                <td>{ssn}</td>
                <td colSpan={6} className="error-text" title={result.message}>
                    {result.message}
                </td>
                <td className="error-text">{result.label}</td>
            </tr>
        );
    }

    const d = result.data;
    return (
        <tr>
            <td>{d.ssn ?? ssn}</td>
            <td>
                {[d.first_name, d.last_name].filter(Boolean).join(" ") || "—"}
            </td>
            <td>{d.address ?? "—"}</td>
            <td>{d.tax_country ?? "—"}</td>
            <td>{d.phone_number ?? "—"}</td>
            <td>{d.email ?? "—"}</td>
            <td>{d.income ?? "—"}</td>
            <td className="ok-text">OK</td>
        </tr>
    );
}
