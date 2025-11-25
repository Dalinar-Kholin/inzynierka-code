import { useState } from "react";

export function useStatusMessages() {
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const showSuccess = (msg: string) => {
        setSuccessMessage(msg);
        setErrorMessage(null);
    };

    const showError = (msg: string) => {
        setErrorMessage(msg);
        setSuccessMessage(null);
    };

    const clearMessages = () => {
        setSuccessMessage(null);
        setErrorMessage(null);
    };

    return {
        successMessage,
        errorMessage,
        showSuccess,
        showError,
        clearMessages,
    };
}