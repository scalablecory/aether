import {Peripheral} from 'react-native-ble-manager';

export interface BlePeripheral extends Peripheral {
  connected?: boolean;
}

export interface BlePeripheralInfo {
  peripheral: string;
  characteristic?: any;
  value?: any;
}

export type BleAction =
  | 'DISCONNECTED'
  | 'DISCOVER'
  | 'RETRIEVE_CONNECTED'
  | 'READ_RSSI'
  | 'RETRIEVE_SERVICES'
  | 'CONNECT_DEVICE'
  | 'SCANNING';

export interface BleCallbackInfo {
  readonly action: BleAction;
  readonly isScanning: boolean;
  readonly results: BlePeripheral[];
}

export type BleCallback = (info: BleCallbackInfo) => void;
