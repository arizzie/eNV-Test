// components/CustomComponent.tsx
import { useState, useEffect } from "react";
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Input,
  Spinner,
} from "@heroui/react";

interface VehicleDataProps {
  onClose: () => void;
  isOpen: boolean;
}

interface BlobStorageInfo {
  BlobName: string;
  Filename: string;
}

// Response from the HTTP Starter function
interface DurableOrchestrationStartResponse {
  Id: string;
  StatusQueryGetUri: string;
  SendEventPostUri: string;
  TerminatePostUri: string;
  PurgeHistoryDeleteUri: string;
}

// Structure of your customStatus object from the orchestrator
interface OrchestratorCustomStatus {
  progress: number;
  message: string;
  completedCount: number;
  totalCount: number;
}

// Full status object from the status polling URI
interface DurableOrchestrationStatus {
  runtimeStatus:
    | "Running"
    | "Pending"
    | "Completed"
    | "Failed"
    | "Terminated"
    | "Canceled";
  output?: any; // The output of your orchestration
  customStatus?: OrchestratorCustomStatus; // Your custom status
  createdTime: string;
  lastUpdatedTime: string;
  // ... other properties you might get (e.g., Input)
}

export default function LoadCsv({ onClose, isOpen }: VehicleDataProps) {
  const [blobStorageInfo, setBlobStorageInfo] = useState<BlobStorageInfo>({
    BlobName: "",
    Filename: "",
  });
  const [orchestrationId, setOrchestrationId] = useState<string | null>(null);
  const [statusUri, setStatusUri] = useState<string | null>(null);
  const [currentStatus, setCurrentStatus] =
    useState<DurableOrchestrationStatus | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Function to initiate the Durable Function orchestration
  const importData = async () => {
    setIsLoading(true);
    setError(null);
    setOrchestrationId(null);
    setStatusUri(null);
    setCurrentStatus(null); // Reset status when starting new process

    try {
      // 1. Make the initial POST request to your ProcessVinStarter
      const response = await fetch(
        "http://localhost:7124/api/StartVinCsvProcessing",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            //'x-functions-key': FUNCTION_KEY, // Include your function key
          },
          body: JSON.stringify({ trigger: "start" }),
        }
      );

      if (!response.ok) {
        throw new Error(
          `HTTP error! Status: ${response.status} - ${response.statusText}`
        );
      }

      const data: DurableOrchestrationStartResponse = await response.json();
      setOrchestrationId(data.Id);
      setStatusUri(data.StatusQueryGetUri);
      console.log("Orchestration started:", data);
    } catch (err: any) {
      setError(`Failed to start orchestration: ${err.message}`);
      console.error("Error starting orchestration:", err);
    }
    finally {
      setIsLoading(false);
    }
  };

  // Effect hook for polling the orchestration status
  useEffect(() => {
    let pollingInterval: NodeJS.Timeout | null = null;

    const pollStatus = async () => {
      if (!statusUri) return; // Don't poll if no URI is set

      try {
        const response = await fetch(statusUri, {
          method: "GET",
          headers: {
            //'x-functions-key': FUNCTION_KEY, // Function key might be needed for status URI too
          },
        });

        if (!response.ok) {
          throw new Error(
            `HTTP error! Status: ${response.status} - ${response.statusText}`
          );
        }

        const data: DurableOrchestrationStatus = await response.json();
        setCurrentStatus(data);
        console.log("Polling status:", data.runtimeStatus, data.customStatus);

        // Stop polling if the orchestration has reached a terminal state
        if (
          data.runtimeStatus === "Completed" ||
          data.runtimeStatus === "Failed" ||
          data.runtimeStatus === "Terminated" ||
          data.runtimeStatus === "Canceled"
        ) {
          if (pollingInterval) {
            clearInterval(pollingInterval);
            pollingInterval = null;
            console.log("Polling stopped. Final status:", data.runtimeStatus);
          }
        }
      } catch (err: any) {
        setError(`Failed to poll status: ${err.message}`);
        console.error("Error polling status:", err);
        if (pollingInterval) {
          clearInterval(pollingInterval);
          pollingInterval = null;
        }
      }
    };

    // Start polling immediately if a statusUri is available
    if (statusUri) {
      pollStatus(); // Initial poll
      pollingInterval = setInterval(pollStatus, 1000); // Poll every 3 seconds
    }

    // Cleanup function: Clear the interval when the component unmounts
    // or when statusUri changes (e.g., if starting a new process)
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [statusUri]); // Re-run this effect whenever statusUri changes

  // Helper to determine progress bar color
  const getProgressBarColor = (progress: number) => {
    if (progress < 25) return "bg-red-500";
    if (progress < 75) return "bg-yellow-500";
    return "bg-green-500";
  };

  // Helper to determine status text color
  const getStatusTextColor = (status: string) => {
    switch (status) {
      case "Running":
      case "Pending":
        return "text-blue-600";
      case "Completed":
        return "text-green-600";
      case "Failed":
      case "Terminated":
      case "Canceled":
        return "text-red-600";
      default:
        return "text-gray-800";
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <ModalContent>
        {(onClose) => (
          <>
            <ModalHeader className="flex flex-col gap-1">
              Import Data
            </ModalHeader>
            <ModalBody>
              <Input
                type="text"
                placeholder="BlobName"
                label="Blob Name"
              ></Input>
              <Input
                type="text"
                placeholder="Filename"
                label="File Name"
              ></Input>
              <Button disabled={isLoading} onPress={importData}>
                Import
              </Button>

              <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
                <div className="bg-white p-8 rounded-lg shadow-xl w-full max-w-2xl">
                  <h1 className="text-3xl font-bold text-center text-gray-800 mb-6">
                    VIN Processing Status
                  </h1>

                  {error && (
                    <p className="mt-4 text-red-500 text-center font-medium">
                      Error: {error}
                    </p>
                  )}

                  {orchestrationId && (
                    <div className="mt-8 p-6 border border-gray-200 rounded-lg bg-gray-50">
                      <h2 className="text-xl font-semibold text-gray-700 mb-4">
                        Orchestration Details:
                      </h2>
                      <p className="text-gray-700 mb-2">
                        <strong>Instance ID:</strong>{" "}
                        <span className="font-mono bg-gray-200 px-2 py-1 rounded text-sm">
                          {orchestrationId}
                        </span>
                      </p>
                      {statusUri && (
                        <p className="text-gray-700 mb-4">
                          <strong>Status URI:</strong>{" "}
                          <a
                            href={statusUri}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-blue-500 hover:underline break-all"
                          >
                            {statusUri}
                          </a>
                        </p>
                      )}

                      {currentStatus ? (
                        <div>
                          <p className="text-gray-700 mb-2">
                            <strong>Runtime Status:</strong>{" "}
                            <span
                              className={`font-bold ${getStatusTextColor(
                                currentStatus.runtimeStatus
                              )}`}
                            >
                              {currentStatus.runtimeStatus}
                            </span>
                          </p>

                          {currentStatus.customStatus ? ( // Use PascalCase property
                            <div className="mt-4">
                              <h3 className="text-lg font-medium text-gray-700 mb-2">
                                Progress:
                              </h3>
                              <div className="w-full bg-gray-200 rounded-full h-4 mb-2 overflow-hidden">
                                <div
                                  className={`h-full text-xs font-medium text-white text-center p-0.5 leading-none rounded-full transition-all duration-500 ease-out ${getProgressBarColor(
                                    currentStatus.customStatus.progress
                                  )}`} // Use PascalCase property
                                  style={{
                                    width: `${currentStatus.customStatus.progress}%`,
                                  }} // Use PascalCase property
                                >
                                  {currentStatus.customStatus.progress}% 
                                </div>
                              </div>
                              <p className="text-gray-700">
                                <strong>Message:</strong>{" "}
                                {currentStatus.customStatus.message} 
                              </p>
                              <p className="text-gray-700 text-sm mt-1">
                                (Processed{" "}
                                {currentStatus.customStatus.CompletedCount} of{" "}
                                {currentStatus.customStatus.TotalCount} batches)
                              </p>
                            </div>
                          ) : (
                            <p className="mt-4 text-gray-600">
                              No custom status available yet...
                            </p>
                          )}

                          {currentStatus.runtimeStatus === "Completed" &&
                            currentStatus.output && ( // Use PascalCase property
                              <div className="mt-4 p-3 bg-green-50 rounded-lg border border-green-200">
                                <h3 className="text-lg font-medium text-green-700 mb-2">
                                  Orchestration Output:
                                </h3>
                                <pre className="text-sm text-green-800 whitespace-pre-wrap break-words">
                                  {JSON.stringify(
                                    currentStatus.output,
                                    null,
                                    2
                                  )}
                                </pre>
                              </div>
                            )}
                          {(currentStatus.runtimeStatus === "Failed" ||
                            currentStatus.runtimeStatus === "Terminated") &&
                            currentStatus.output && ( // Use PascalCase property
                              <div className="mt-4 p-3 bg-red-50 rounded-lg border border-red-200">
                                <h3 className="text-lg font-medium text-red-700 mb-2">
                                  Error Details:
                                </h3>
                                <pre className="text-sm text-red-800 whitespace-pre-wrap break-words">
                                  {JSON.stringify(
                                    currentStatus.output,
                                    null,
                                    2
                                  )}
                                </pre>
                              </div>
                            )}
                        </div>
                      ) : (
                        <p className="text-gray-600">
                          Waiting for initial status...
                        </p>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </ModalBody>
            <ModalFooter>
              <Button color="danger" variant="light" onPress={onClose}>
                Close
              </Button>
            </ModalFooter>
          </>
        )}
      </ModalContent>
    </Modal>
  );
}
