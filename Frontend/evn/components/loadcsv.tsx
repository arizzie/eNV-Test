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
  ContainerName: string;
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
  Progress: number;
  Message: string;
  CompletedCount: number;
  TotalCount: number;
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
  output?: any; 
  customStatus?: OrchestratorCustomStatus; 
  createdTime: string;
  lastUpdatedTime: string;
}

export default function LoadCsv({ onClose, isOpen }: VehicleDataProps) {
  const [blobStorageInfo, setBlobStorageInfo] = useState<BlobStorageInfo>({
    ContainerName: "",
    Filename: "",
  });
  const [orchestrationId, setOrchestrationId] = useState<string | null>(null);
  const [statusUri, setStatusUri] = useState<string | null>(null);
  const [currentStatus, setCurrentStatus] =
    useState<DurableOrchestrationStatus | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Helper function to update blobStorageInfo
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setBlobStorageInfo((prevInfo) => ({
      ...prevInfo,
      [name]: value,
    }));
  };

  // Function to initiate the Durable Function orchestration
  const importData = async () => {
    setIsLoading(true);
    setError(null);
    setOrchestrationId(null); // Reset ID/status from previous run if any
    setStatusUri(null);
    setCurrentStatus(null);

    const backendApiUrl = process.env.NEXT_PUBLIC_BACKEND_URL;

    try {
      // Make the initial POST request to your ProcessVinStarter
      const response = await fetch(
        `${backendApiUrl}api/StartVinCsvProcessing`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            //'x-functions-key': FUNCTION_KEY, // Include your function key if needed
          },
          body: JSON.stringify(blobStorageInfo),
        }
      );

      if (!response.ok) {
        let errorMessage = `HTTP error! Status: ${response.status} - ${response.statusText}`;
        try {
            const errorBody = await response.json();
            if (errorBody.message) {
                errorMessage = errorBody.message;
            }
        } catch (parseError) {
            // Ignore if response body isn't JSON
        }
        throw new Error(errorMessage);
      }

      const data: DurableOrchestrationStartResponse = await response.json();
      setOrchestrationId(data.Id);
      setStatusUri(data.StatusQueryGetUri);
      console.log("Orchestration started:", data);
    } catch (err: any) {
      setError(`Failed to start orchestration: ${err.message}`);
      console.error("Error starting orchestration:", err);
    } finally {
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

  // --- NEW: Function to handle modal close and reset state ---
  const handleModalClose = () => {
    // Reset all status-related state
    setOrchestrationId(null);
    setStatusUri(null);
    setCurrentStatus(null);
    setError(null);
    // Also reset input fields for the next time the modal opens
    setBlobStorageInfo({ ContainerName: "", Filename: "" });

    // Call the original onClose prop to dismiss the modal
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleModalClose} size="4xl" scrollBehavior="inside">
      <ModalContent>
        {() => (
          <>
            <ModalHeader className="flex flex-col gap-1 text-2xl font-semibold text-gray-800">
              Import Data
            </ModalHeader>

            <ModalBody className="py-4 px-6 flex flex-col gap-6">
              <div className="flex flex-col gap-4">
                <Input
                  type="text"
                  name="ContainerName"
                  placeholder="e.g., evn-test"
                  label="Container Name"
                  labelPlacement="outside"
                  fullWidth
                  value={blobStorageInfo.ContainerName}
                  onChange={handleInputChange}
                  classNames={{
                    inputWrapper: "border shadow-sm",
                    input: "placeholder:text-gray-400"
                  }}
                />
                <Input
                  type="text"
                  name="Filename"
                  placeholder="e.g., sample-vin-data.csv"
                  label="File Name"
                  labelPlacement="outside"
                  fullWidth
                  value={blobStorageInfo.Filename}
                  onChange={handleInputChange}
                  classNames={{
                    inputWrapper: "border shadow-sm",
                    input: "placeholder:text-gray-400"
                  }}
                />
              </div>

              <Button
                disabled={isLoading}
                onPress={importData}
                color="primary"
                size="lg"
                className="w-full"
              >
                {isLoading ? 'Importing...' : 'Import'}
              </Button>

              {orchestrationId && (
                <div className="bg-white p-6 rounded-lg shadow-md border border-gray-100">
                  <h1 className="text-2xl font-bold text-center text-gray-800 mb-6">
                    VIN Processing Status
                  </h1>

                  {error && (
                    <p className="mt-4 text-red-600 text-center font-medium whitespace-pre-wrap break-words">
                      Error: {error}
                    </p>
                  )}

                  {orchestrationId && ( // This inner check is redundant if outer check is orchestrationId, but harmless
                    <div className="mt-6 p-4 border border-gray-200 rounded-lg bg-gray-50 flex flex-col gap-3">
                      <h2 className="text-lg font-semibold text-gray-700">
                        Orchestration Details:
                      </h2>
                      <p className="text-gray-700">
                        <strong>Instance ID:</strong>{" "}
                        <span className="font-mono bg-gray-200 px-2 py-1 rounded text-sm break-all">
                          {orchestrationId}
                        </span>
                      </p>
                      {statusUri && (
                        <p className="text-gray-700">
                          <strong>Status URI:</strong>{" "}
                          <a
                            href={statusUri}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-blue-600 hover:underline break-all"
                          >
                            {statusUri}
                          </a>
                        </p>
                      )}

                      {currentStatus ? (
                        <div className="mt-3 flex flex-col gap-2">
                          <p className="text-gray-700">
                            <strong>Runtime Status:</strong>{" "}
                            <span
                              className={`font-bold ${getStatusTextColor(
                                currentStatus.runtimeStatus
                              )}`}
                            >
                              {currentStatus.runtimeStatus}
                            </span>
                          </p>

                          {currentStatus.customStatus ? (
                            <div className="mt-3">
                              <h3 className="text-base font-medium text-gray-700 mb-2">
                                Progress:
                              </h3>
                              <div className="w-full bg-gray-200 rounded-full h-4 mb-2 overflow-hidden">
                                <div
                                  className={`h-full text-xs font-medium text-white text-center p-0.5 leading-none rounded-full transition-all duration-500 ease-out ${getProgressBarColor(
                                    currentStatus.customStatus.Progress
                                  )}`}
                                  style={{
                                    width: `${currentStatus.customStatus.Progress}%`,
                                  }}
                                >
                                  {currentStatus.customStatus.Progress}%
                                </div>
                              </div>
                              <p className="text-gray-700 whitespace-normal break-words">
                                <strong>Message:</strong>{" "}
                                {currentStatus.customStatus.Message}
                              </p>
                              <p className="text-gray-700 text-sm mt-1">
                                (Processed{" "}
                                {currentStatus.customStatus.CompletedCount} of{" "}
                                {currentStatus.customStatus.TotalCount} batches)
                              </p>
                            </div>
                          ) : (
                            <p className="mt-3 text-gray-600 italic">
                              No custom status available yet...
                            </p>
                          )}

                          {currentStatus.runtimeStatus === "Completed" &&
                            currentStatus.output && (
                              <div className="mt-4 p-3 bg-green-50 rounded-lg border border-green-200 overflow-auto max-h-48">
                                <h3 className="text-base font-medium text-green-700 mb-2">
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
                            currentStatus.output && (
                              <div className="mt-4 p-3 bg-red-50 rounded-lg border border-red-200 overflow-auto max-h-48">
                                <h3 className="text-base font-medium text-red-700 mb-2">
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
                        <p className="text-gray-600 italic">
                          Waiting for initial status...
                        </p>
                      )}
                    </div>
                  )}
                </div>
              )}
            </ModalBody>

            <ModalFooter className="flex justify-end p-4 border-t border-gray-100">
              <Button color="danger" variant="light" onPress={handleModalClose}>
                Close
              </Button>
            </ModalFooter>
          </>
        )}
      </ModalContent>
    </Modal>
  );
}