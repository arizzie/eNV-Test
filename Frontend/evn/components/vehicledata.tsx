// components/CustomComponent.tsx
import { useState, useEffect } from 'react';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
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
      CarId: number;
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

  useEffect(() => {
    const fetchData = async () => {
    if(isOpen){
        setLoading(true);
        try {
            const response = await fetch(`http://localhost:7124/api/vins/${vin}`);
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
     <Modal isOpen={isOpen} onClose={onClose}>
        <ModalContent>
          {(onClose) => (
            <>
              <ModalHeader className="flex flex-col gap-1">{vin}</ModalHeader>
              <ModalBody>
                {loading ? (
                  <div>Loading...</div>
                ) : (
                  <div className="flex flex-col gap-2">
                    {data?.AdditionalVehicleInfo.map((item) => (
                      <div key={item.VariableId} className="flex flex-col">
                        <span>{item.Variable.Name} : </span>
                        <span>{item.Value}</span>
                      </div>
                    ))}
                  </div>
                )}
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