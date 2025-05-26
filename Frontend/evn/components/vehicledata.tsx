// components/CustomComponent.tsx
import { useState, useEffect } from 'react';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Spinner,
} from "@heroui/react";

interface VehicleDataProps {
  vin: string;
  onClose: () => void;
  isOpen: boolean;
}

interface VehicleData{
    Vin: string;
    DealerId: string;
    ModifiedDate: Date;
    AdditionalVehicleInfo: {
      Value: string;
      VariableId: number;
      VehicleId: number;
      Variable: {
        Id: number;
        Name: string;
      }
    }[];
}

export default function VehicleData({ vin, onClose, isOpen }: VehicleDataProps) {
  const [data, setData] = useState<VehicleData | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  const backendApiUrl = process.env.NEXT_PUBLIC_BACKEND_URL;

  useEffect(() => {
    const fetchData = async () => {
    if(isOpen){
        setLoading(true);
        try {
            const response = await fetch(`${backendApiUrl}api/vins/${vin}`);
            const data = await response.json();
            setData(data);
        } catch (error) {
            setError(error);
        } finally {
            setLoading(false);
        }
        };
    };
    fetchData();
  }, [isOpen, vin]);

  return (
     <Modal isOpen={isOpen} onClose={onClose} size="lg"> {/* Added size="lg" for a slightly larger modal */}
      <ModalContent>
        {() => ( // The onClose prop from ModalContent is typically used here if you need to close the modal from internal elements.
          <>
            {/* Modal Header: Bold VIN, better padding/separator */}
            <ModalHeader className="flex flex-col gap-1 pb-4 text-xl font-semibold border-b border-gray-100">
              Vehicle Details for <span className="font-bold text-2xl">{vin}</span>
            </ModalHeader>

            {/* Modal Body: Loading, Error, No Data, or Details List */}
            <ModalBody className="py-6 px-8 flex flex-col gap-4"> {/* Increased padding, adjusted gap */}
              {loading ? (
                <div className="flex justify-center items-center h-32">
                  <Spinner size="lg" /> {/* Larger spinner for loading state */}
                </div>
              ) : error ? ( // Display error message
                <div className="text-red-600 text-center py-4">
                  <p className="font-medium">Error loading data:</p>
                  <p className="text-sm">{error}</p>
                </div>
              ) : (
                <div className="flex flex-col gap-3"> {/* Gap between each detail row */}
                  {data?.AdditionalVehicleInfo.length === 0 ? (
                    <span className="text-gray-600 italic text-center py-4">
                      No additional vehicle information available.
                    </span>
                  ) : (
                    data?.AdditionalVehicleInfo.map((item) => (
                      <div key={item.VariableId} className="flex items-start gap-2 text-base text-gray-800"> {/* Flex row for key-value, align items-start if values can wrap */}
                        <span className="font-semibold text-gray-900 w-40 flex-shrink-0"> {/* Bold and fixed width for variable name */}
                          {item.Variable.Name}:
                        </span>
                        <span className="flex-grow"> {/* Value takes remaining space */}
                          {item.Value || "N/A"} {/* Display N/A if value is empty */}
                        </span>
                      </div>
                    ))
                  )}
                </div>
              )}
            </ModalBody>

            {/* Modal Footer: Close Button */}
            <ModalFooter className="flex justify-end p-4 border-t border-gray-100"> {/* Consistent footer styling */}
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