/**
 * BLE Service - Bluetooth Low Energy module
 *
 * This module is a wrapper to react-native-ble-manager librairie.
 * https://www.npmjs.com/package/react-native-ble-manager
 */
import {
  NativeModules,
  NativeEventEmitter,
  EmitterSubscription,
} from 'react-native';
import BleManager from 'react-native-ble-manager';
import {
  BleAction,
  BlePeripheral,
  BlePeripheralInfo,
  BleCallback,
  BleCallbackInfo,
} from './BleService.types';

const BleManagerModule = NativeModules.BleManager;
const bleManagerEmitter = new NativeEventEmitter(BleManagerModule);

const logData = (title: string, data?: any) => {
  console.log('=============================\n');
  console.log(title.toLocaleUpperCase(), data ?? '');
  console.log('=============================\n');
};

export class BleService {
  private isScanning: boolean;
  private peripherals: Map<string, BlePeripheral>;
  private subscriptions: EmitterSubscription[];
  private onEvent: BleCallback;

  constructor(onEvent: BleCallback) {
    this.isScanning = false;
    this.peripherals = new Map<string, BlePeripheral>();
    this.subscriptions = [];
    this.onEvent = onEvent;
  }

  /**
   * Initialize bluetooth events
   */
  initialize = () => {
    BleManager.start({showAlert: false});

    this.subscriptions.push(
      bleManagerEmitter.addListener(
        'BleManagerDiscoverPeripheral',
        this.handleDiscoverPeripheral,
      ),
    );

    this.subscriptions.push(
      bleManagerEmitter.addListener('BleManagerStopScan', this.handleStopScan),
    );

    this.subscriptions.push(
      bleManagerEmitter.addListener(
        'BleManagerDisconnectPeripheral',
        this.handleDisconnectedPeripheral,
      ),
    );

    this.subscriptions.push(
      bleManagerEmitter.addListener(
        'BleManagerDidUpdateValueForCharacteristic',
        this.handleUpdateValueForCharacteristic,
      ),
    );
  };

  /**
   * Cancel all bluetooth subscriptions events
   */
  remove = () => {
    this.subscriptions.forEach(s => s.remove());
  };

  /**
   * Indicates if the bluetooth scanning is currently in progress or not.
   */
  getIsScanning = (): boolean => this.isScanning;

  /**
   * Set the value of bluetooth scanning flag.
   * @param isScanning
   */
  setIsScanning = (isScanning: boolean) => {
    this.isScanning = isScanning;
    this.runCallback('SCANNING');
  };

  /**
   * Get the list of peripherals available after bluetooth scanning on the device.
   */
  getPeripherals = (): BlePeripheral[] => {
    return Array.from(this.peripherals.values());
  };

  /**
   * Execute the callback function to return the peripheral list.
   */
  runCallback = (action: BleAction) => {
    const info: BleCallbackInfo = {
      action,
      isScanning: this.getIsScanning(),
      results: this.getPeripherals(),
    };
    logData('Callback info: ', info);
    this.onEvent(info);
  };

  /**
   * Start bluetooth scanning on the device.
   * @param seconds - Number of seconds to scan
   */
  startScan = (seconds: number) => {
    if (!this.isScanning) {
      BleManager.scan([], seconds, true)
        .then(() => {
          logData('Scanning...');
          this.setIsScanning(true);
        })
        .catch(err => {
          console.error(err);
        });
    }
  };

  /**
   * Handle stop scanning event
   */
  handleStopScan = () => {
    logData('Scan is stopped');
    this.setIsScanning(false);
  };

  /**
   * Handle Disconnecting a peripheral event
   * @param data - Peripheral info from the event emitter
   */
  handleDisconnectedPeripheral = (data: BlePeripheralInfo) => {
    logData('handleDisconnectedPeripheral', data);
    let peripheral = this.peripherals.get(data.peripheral);
    if (peripheral) {
      peripheral.connected = false;
      this.peripherals.set(peripheral.id, peripheral);
      this.runCallback('DISCONNECTED');
    }
    logData('Disconnected from ' + data.peripheral);
  };

  /**
   * Handle update value for characteristic
   * @param data
   */
  handleUpdateValueForCharacteristic = (data: BlePeripheralInfo) => {
    logData(
      'Received data from ' +
        data.peripheral +
        ' characteristic ' +
        data.characteristic,
      data.value,
    );
  };

  /**
   * Handle discover peripheral event.
   *
   * @param peripheral
   */
  handleDiscoverPeripheral = (peripheral: BlePeripheral) => {
    logData('Got ble peripheral', peripheral);
    if (!peripheral.name) {
      peripheral.name = peripheral.id;
    }
    this.peripherals.set(peripheral.id, peripheral);
    this.runCallback('DISCOVER');
  };

  /**
   * Retrieve the list of connected bluetooth devices.
   */
  retrieveConnected = () => {
    BleManager.getConnectedPeripherals([]).then(results => {
      if (results.length == 0) {
        logData('No connected peripherals');
      }

      logData('retrieveConnected: ', results);

      for (var i = 0; i < results.length; i++) {
        var peripheral = results[i];
        this.peripherals.set(peripheral.id, {...peripheral, connected: true});
      }

      this.runCallback('RETRIEVE_CONNECTED');
    });
  };

  /**
   * Connect the peripheral device.
   */
  connectDevice = (id: string) => {
    logData('connecting with device', id);
    BleManager.connect(id)
      .then(() => {
        logData('connect with device', id);
        let p = this.peripherals.get(id);
        if (p) {
          p.connected = true;
          this.peripherals.set(id, p);
          this.runCallback('CONNECT_DEVICE');
        }
        logData('Connected to ' + id);
      })
      .catch(error => {
        logData('Connection error', error);
      });
  };

  /**
   * Retrieve the peripheral services.
   */
  retrieveServices = (peripheral: BlePeripheral) => {
    BleManager.retrieveServices(peripheral.id).then(peripheralData => {
      logData('Retrieved peripheral services', peripheralData);
      this.runCallback('RETRIEVE_SERVICES');
    });
  };

  /**
   * Read the peripheral RSSI.
   */
  readRSSI = (id: string) => {
    BleManager.readRSSI(id).then(rssi => {
      logData('Retrieved actual RSSI value', rssi);
      let p = this.peripherals.get(id);
      if (p) {
        //p.rssi = rssi;
        this.peripherals.set(id, p);
        this.runCallback('READ_RSSI');
      }
    });
  };

  testPeripheral = (peripheral: BlePeripheral) => {
    logData('testPeripheral', peripheral);
    if (peripheral) {
      if (peripheral.connected) {
        BleManager.disconnect(peripheral.id);
      } else {
        this.connectDevice(peripheral.id);
      }
    }
  };
}

// Test using bleno's pizza example
// https://github.com/sandeepmistry/bleno/tree/master/examples/pizza
/*
BleManager.retrieveServices(peripheral.id).then(peripheralInfo => {
  logData(peripheralInfo);
  var service = '13333333-3333-3333-3333-333333333337';
  var bakeCharacteristic = '13333333-3333-3333-3333-333333330003';
  var crustCharacteristic = '13333333-3333-3333-3333-333333330001';

  setTimeout(() => {
    BleManager.startNotification(peripheral.id, service, bakeCharacteristic)
      .then(() => {
        logData('Started notification on ' + peripheral.id);
        setTimeout(() => {
          BleManager.write(peripheral.id, service, crustCharacteristic, [
            0,
          ]).then(() => {
            logData('Writed NORMAL crust');
            BleManager.write(
              peripheral.id,
              service,
              bakeCharacteristic,
              [1, 95],
            ).then(() => {
              logData('Writed 351 temperature, the pizza should be BAKED');

              //var PizzaBakeResult = {
              //  HALF_BAKED: 0,
              //  BAKED:      1,
              //  CRISPY:     2,
              //  BURNT:      3,
              //  ON_FIRE:    4
              //};
            });
          });
        }, 500);
      })
      .catch(error => {
        logData('Notification error', error);
      });
  }, 200);
}); */
